using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;
using MOHProject.Domain.ValueObjects;
using MOHProject.Infrastructure.Persistence;
using MOHProject.Infrastructure.Reminders;

namespace MOHProject.Tests.Integration.Application;

// Verifies Phase 5 wiring end-to-end:
//   - UwDecisionCommand emits LOA with shortfall → reminders are scheduled
//   - Subsequent supersession cancels the old reminders
//   - Substatus → PendingUwAps cancels all remaining reminders
[Collection(nameof(SqlServerCollection))]
public class RemindersE2ETests
{
    private readonly SqlServerFixture _fixture;

    public RemindersE2ETests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ExclusionRider_ScheduledClosedown_Then_SubstandardAgain_SupersedesOldReminders()
    {
        // Setup: entry CondAccept, ExclusionOnly composition, shortfall > 0.
        // UW Standard → evaluator keeps CondAccept substatus, emits CLOA (Exclusion).
        // → reminders scheduled from that CLOA.
        // Then repeat with UW Substandard → new CLOA v2 emitted → v1 superseded → v1's reminders cancelled.
        var policyId = await CreateCondAcceptPolicy(withShortfall: true, riderExclusion: true);

        await using (var db = _fixture.CreateContext())
        {
            var sut = BuildSut(db);
            await sut.HandleAsync(new UwDecisionCommand(policyId, UwDecision.Standard, "u1"), default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var reminders = await db.Reminders.Where(r => r.PolicyId == policyId).ToListAsync();
            reminders.Should().HaveCount(2, "one CloaReminder + one CloaFinalReminder from the first CLOA");
            reminders.Should().OnlyContain(r => r.Status == ReminderStatus.Scheduled);
        }

        // Second decision → generates a new CLOA of the same type → supersedes v1.
        await using (var db = _fixture.CreateContext())
        {
            var sut = BuildSut(db);
            await sut.HandleAsync(new UwDecisionCommand(policyId, UwDecision.Standard, "u2"), default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var reminders = await db.Reminders
                .Where(r => r.PolicyId == policyId)
                .OrderBy(r => r.Id)
                .ToListAsync();

            reminders.Should().HaveCount(4, "2 old + 2 new — cancellation is soft");
            reminders.Take(2).Should().OnlyContain(r => r.Status == ReminderStatus.Cancelled,
                "the first CLOA was superseded → its reminders were cancelled during LetterGenerator supersession");
            reminders.Skip(2).Should().OnlyContain(r => r.Status == ReminderStatus.Scheduled,
                "the new CLOA scheduled a fresh reminder pair");
        }
    }

    [Fact]
    public async Task Aps_ForcesPendingUwAps_CancelsAllReminders()
    {
        var policyId = await CreateCondAcceptPolicy(withShortfall: true, riderExclusion: true);

        // First call schedules reminders from the CLOA.
        await using (var db = _fixture.CreateContext())
            await BuildSut(db).HandleAsync(new UwDecisionCommand(policyId, UwDecision.Standard, "u1"), default);

        await using (var db = _fixture.CreateContext())
            (await db.Reminders.Where(r => r.PolicyId == policyId && r.Status == ReminderStatus.Scheduled).CountAsync())
                .Should().Be(2, "sanity check: reminders exist before the APS transition");

        // APS at CondAccept → substatus → PendingUwAps → cancel all.
        await using (var db = _fixture.CreateContext())
            await BuildSut(db).HandleAsync(new UwDecisionCommand(policyId, UwDecision.Aps, "u2"), default);

        await using (var db = _fixture.CreateContext())
        {
            var reminders = await db.Reminders.Where(r => r.PolicyId == policyId).ToListAsync();
            reminders.Should().OnlyContain(r => r.Status == ReminderStatus.Cancelled,
                "PendingUwAps stops ALL LOA/CLOA reminders — source lines 363-365");
        }
    }

    // ------------------------------------------------------------------

    private UwDecisionCommandHandler BuildSut(AppDbContext db)
    {
        var scheduler = new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions()));
        IPolicyRepository repo = new EfPolicyRepository(db);
        IAuditTrailWriter audit = new EfAuditTrailWriter(db);
        ILetterGenerator letters = new EfLetterGenerator(db, scheduler);
        IUnitOfWork uow = new EfUnitOfWork(db);

        var registry = new EntryPointHandlerRegistry(new IEntryPointHandler[]
        {
            new CondAcceptLetterGenHandler(),
            new PendingUwCloaAssessmentHandler(),
            new PendingCashCollectionHandler(),
            new PendingIpRequestFileHandler(),
            new PendingIpResponseCpfRejectedHandler(),
            new PendingPpRequestFileHandler(),
            new PendingPpResponseFileCpfRejectedHandler(),
        });
        IRemainingPlansEvaluator evaluator = new RemainingPlansEvaluator(
            new PlansCompositionEvaluator(),
            new UwFieldStatesEvaluator(),
            new NextSubstatusEvaluator(),
            new LetterTypeEvaluator(),
            NullLogger<RemainingPlansEvaluator>.Instance);

        return new UwDecisionCommandHandler(repo, registry, evaluator, letters, audit, uow, scheduler);
    }

    private async Task<long> CreateCondAcceptPolicy(bool withShortfall, bool riderExclusion)
    {
        var policyNumber = $"P-{Guid.NewGuid():N}"[..30];
        await using var db = _fixture.CreateContext();
        var policy = new Policy
        {
            PolicyNumber = policyNumber,
            Type = PolicyType.NewBusiness,
            Substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated,
            InsuredResidency = Residency.Sg,
            PayerResidency = Residency.Sg,
            UWState = new UWState { AcceptCloa = AcceptCloa.Blank },
            PremiumCollection = withShortfall
                ? new PremiumCollection { LinkedRidersToCollect = new Money(500m), LinkedRidersCollected = new Money(250m) }
                : new PremiumCollection(),
        };
        policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active });
        if (riderExclusion)
            policy.Plans.Add(new Plan { IsBase = false, ProductCode = "Rider", Status = ProductStatus.Active, HasActiveExclusion = true });
        db.Policies.Add(policy);
        await db.SaveChangesAsync();
        return policy.Id;
    }
}
