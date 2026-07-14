using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;
using MOHProject.Infrastructure.Persistence;
using MOHProject.Infrastructure.Reminders;

namespace MOHProject.Tests.Integration.Application;

// PH-06-T01 — the 4 UAT bugs from docs/specs/bugs/uat-2026-06.md reproduced
// through the FULL command pipeline (repository + evaluator + letter + reminder
// + audit + transaction), not just the evaluator in isolation. Complements the
// evaluator-only regression suite in MOHProject.Tests/Regression/UatBugs/.
//
// Definition of Done for the bugs: substatus + letter + UWState fields + audit
// all persisted in one atomic call.
[Collection(nameof(SqlServerCollection))]
public class UatBugsE2ETests
{
    private readonly SqlServerFixture _fixture;

    public UatBugsE2ETests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Bug_2610000310P_NtuPremierExclusion_PersistsPendingCashAndNewLoa_ThroughCommand()
    {
        // Setup: Base=Std, Premier=Exclusion (CLOA outstanding), Choice=Declined, CancerGuard=Std.
        // Substatus = CondAccept. Shortfall > 0.
        long policyId, premierId;
        (policyId, premierId) = await SeedPolicyAsync(async db =>
        {
            var policy = new Policy
            {
                PolicyNumber = $"P-310P-{Guid.NewGuid():N}"[..30],
                Type = PolicyType.NewBusiness,
                Substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated,
                InsuredResidency = Residency.Sg,
                PayerResidency = Residency.Sg,
                UWState = new UWState { AcceptCloa = AcceptCloa.Yes },
                PremiumCollection = new PremiumCollection
                {
                    BaseToCollect = new Money(1200m), BaseCollected = new Money(1200m),
                    LinkedRidersToCollect = new Money(500m), LinkedRidersCollected = new Money(250m), // $250 shortfall
                },
            };
            policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active, GrossPremium = new Money(1200m) });
            policy.Plans.Add(new Plan { IsBase = false, ProductCode = "CancerGuard", Status = ProductStatus.Active, GrossPremium = new Money(200m) });
            policy.Plans.Add(new Plan { IsBase = false, ProductCode = "Choice", Status = ProductStatus.Declined });
            var premier = new Plan { IsBase = false, ProductCode = "Premier", Status = ProductStatus.Active, HasActiveExclusion = true, GrossPremium = new Money(300m) };
            policy.Plans.Add(premier);
            db.Policies.Add(policy);
            await db.SaveChangesAsync();
            return (policy.Id, premier.Id);
        });

        // Action: NTU Premier.
        await using (var db = _fixture.CreateContext())
            await BuildSut(db).HandleAsync(new MarkRiderStatusCommand(policyId, premierId, ProductStatus.NotTakenUp, "u"), default);

        // Expected persistence: PendingCashCollection + LOA row + audit row.
        await using (var db = _fixture.CreateContext())
        {
            var policy = await db.Policies.Include(p => p.UWState).SingleAsync(p => p.Id == policyId);
            policy.Substatus.Should().Be(PolicySubstatus.PendingCashCollection,
                "BUG-2610000310P was 'stuck at CondAccept' — must be PendingCashCollection after fix");
            policy.UWState!.RcmpFlag.Should().BeFalse();
            policy.UWState.AcceptCloaEnabled.Should().BeFalse("greyed when composition is AllStandard");

            var letters = await db.Letters.Where(l => l.PolicyId == policyId).ToListAsync();
            letters.Should().Contain(l => l.Type == LetterType.NtuWithoutRefund && l.IsCurrent,
                "decision-specific NTU letter emitted");
            letters.Should().Contain(l => l.Type == LetterType.Loa && l.IsCurrent,
                "new LOA emitted (composition swings to AllStandard at PendingCash — letter-gating substatus)");

            var audits = await db.AuditEntries.Where(a => a.PolicyId == policyId).ToListAsync();
            audits.Should().ContainSingle().Which.EventType.Should().Be(MarkRiderStatusCommandHandler.AuditEventType);
        }
    }

    [Fact]
    public async Task Bug_2610000154P_NtuChoiceRcmp_ResetsRcmpFields_MovesTo_PendingIpRequestFile()
    {
        // Setup: Base=Std, Choice=RCMP (loading+exclusion). AcceptCloa=Yes, RcmpOption=Option1.
        // Substatus = PendingCashCollection. Shortfall = 0 (already collected).
        long policyId, choiceId;
        (policyId, choiceId) = await SeedPolicyAsync(async db =>
        {
            var policy = new Policy
            {
                PolicyNumber = $"P-154P-{Guid.NewGuid():N}"[..30],
                Type = PolicyType.NewBusiness,
                Substatus = PolicySubstatus.PendingCashCollection,
                InsuredResidency = Residency.Sg,
                PayerResidency = Residency.Sg,
                UWState = new UWState
                {
                    RcmpFlag = true, RcmpFlagEnabled = true,
                    AcceptCloa = AcceptCloa.Yes, AcceptCloaEnabled = true,
                    RcmpOption = RcmpOption.Option1, RcmpOptionEnabled = true,
                    CompleteUw = true,
                },
                PremiumCollection = new PremiumCollection(),
            };
            policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active });
            var choice = new Plan { IsBase = false, ProductCode = "Choice", Status = ProductStatus.Active, HasActiveRiskLoading = true, HasActiveExclusion = true };
            policy.Plans.Add(choice);
            db.Policies.Add(policy);
            await db.SaveChangesAsync();
            return (policy.Id, choice.Id);
        });

        await using (var db = _fixture.CreateContext())
            await BuildSut(db).HandleAsync(new MarkRiderStatusCommand(policyId, choiceId, ProductStatus.NotTakenUp, "u"), default);

        await using (var db = _fixture.CreateContext())
        {
            var policy = await db.Policies.Include(p => p.UWState).SingleAsync(p => p.Id == policyId);
            policy.Substatus.Should().Be(PolicySubstatus.PendingIpRequestFile,
                "BUG-2610000154P was 'stuck at PendingCashCollection' — must move to PendingIpRequestFile");
            policy.UWState!.RcmpFlag.Should().BeFalse("BUG was: RCMP flag remained ticked");
            policy.UWState.AcceptCloa.Should().Be(AcceptCloa.Blank, "BUG was: AcceptCloa remained Yes");
            policy.UWState.RcmpOption.Should().Be(RcmpOption.Blank, "BUG was: RcmpOption remained Option1");

            (await db.Letters.CountAsync(l => l.PolicyId == policyId && l.Type == LetterType.NtuWithoutRefund))
                .Should().Be(1, "NTU letter emitted");
        }
    }

    [Fact]
    public async Task Bug_2610000345P_NtuPremier_AutoSelectsCompleteUw_MovesTo_PendingCashCollection()
    {
        // Setup: Base=Std, Choice=Std. Then Premier added with medical condition,
        // Substatus = PendingManualUnderwriting.
        long policyId, premierId;
        (policyId, premierId) = await SeedPolicyAsync(async db =>
        {
            var policy = new Policy
            {
                PolicyNumber = $"P-345P-{Guid.NewGuid():N}"[..30],
                Type = PolicyType.NewBusiness,
                Substatus = PolicySubstatus.PendingManualUnderwriting,
                InsuredResidency = Residency.Sg,
                PayerResidency = Residency.Sg,
                UWState = new UWState { CompleteUw = false, AcceptCloa = AcceptCloa.Blank },
                PremiumCollection = new PremiumCollection
                {
                    LinkedRidersToCollect = new Money(300m), LinkedRidersCollected = new Money(200m),
                },
            };
            policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active });
            policy.Plans.Add(new Plan { IsBase = false, ProductCode = "Choice", Status = ProductStatus.Active });
            var premier = new Plan { IsBase = false, ProductCode = "Premier", Status = ProductStatus.Active, GrossPremium = new Money(100m) };
            policy.Plans.Add(premier);
            db.Policies.Add(policy);
            await db.SaveChangesAsync();
            return (policy.Id, premier.Id);
        });

        await using (var db = _fixture.CreateContext())
            await BuildSut(db).HandleAsync(new MarkRiderStatusCommand(policyId, premierId, ProductStatus.NotTakenUp, "u"), default);

        await using (var db = _fixture.CreateContext())
        {
            var policy = await db.Policies.Include(p => p.UWState).SingleAsync(p => p.Id == policyId);
            policy.Substatus.Should().Be(PolicySubstatus.PendingCashCollection,
                "BUG-2610000345P was 'stuck at PendingManualUnderwriting'");
            policy.UWState!.CompleteUw.Should().BeTrue("BUG was: Complete UW button not selected");
        }
    }

    [Fact]
    public async Task Bug_2610000346P_NtuChoiceExclusion_MovesTo_PendingCash_And_EmitsNewLoa()
    {
        // Setup: Base=Std, CancerGuard=Std, Choice=Exclusion. Substatus=CondAccept.
        long policyId, choiceId;
        (policyId, choiceId) = await SeedPolicyAsync(async db =>
        {
            var policy = new Policy
            {
                PolicyNumber = $"P-346P-{Guid.NewGuid():N}"[..30],
                Type = PolicyType.NewBusiness,
                Substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated,
                InsuredResidency = Residency.Sg,
                PayerResidency = Residency.Sg,
                UWState = new UWState { AcceptCloa = AcceptCloa.Blank, CompleteUw = false },
                PremiumCollection = new PremiumCollection
                {
                    LinkedRidersToCollect = new Money(400m), LinkedRidersCollected = new Money(250m), // shortfall
                },
            };
            policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active });
            policy.Plans.Add(new Plan { IsBase = false, ProductCode = "CancerGuard", Status = ProductStatus.Active });
            var choice = new Plan { IsBase = false, ProductCode = "Choice", Status = ProductStatus.Active, HasActiveExclusion = true };
            policy.Plans.Add(choice);
            db.Policies.Add(policy);
            await db.SaveChangesAsync();
            return (policy.Id, choice.Id);
        });

        await using (var db = _fixture.CreateContext())
            await BuildSut(db).HandleAsync(new MarkRiderStatusCommand(policyId, choiceId, ProductStatus.NotTakenUp, "u"), default);

        await using (var db = _fixture.CreateContext())
        {
            var policy = await db.Policies.Include(p => p.UWState).SingleAsync(p => p.Id == policyId);
            policy.Substatus.Should().Be(PolicySubstatus.PendingCashCollection);
            policy.UWState!.CompleteUw.Should().BeTrue();

            (await db.Letters.CountAsync(l => l.PolicyId == policyId && l.Type == LetterType.Loa && l.IsCurrent))
                .Should().Be(1, "BUG-2610000346P was 'no new NB LOA generated'");
        }
    }

    // ------------------------------------------------------------------

    private async Task<(long PolicyId, long PlanId)> SeedPolicyAsync(Func<AppDbContext, Task<(long, long)>> seed)
    {
        await using var db = _fixture.CreateContext();
        return await seed(db);
    }

    private MarkRiderStatusCommandHandler BuildSut(AppDbContext db)
    {
        var scheduler = new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions()));
        var evaluator = new RemainingPlansEvaluator(
            new PlansCompositionEvaluator(),
            new UwFieldStatesEvaluator(),
            new NextSubstatusEvaluator(),
            new LetterTypeEvaluator(),
            NullLogger<RemainingPlansEvaluator>.Instance);
        return new MarkRiderStatusCommandHandler(
            new EfPolicyRepository(db),
            evaluator,
            new EfLetterGenerator(db, scheduler),
            new EfAuditTrailWriter(db),
            new EfUnitOfWork(db),
            scheduler);
    }
}
