using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MOHProject.Application.Features.AdditionOfRider;
using MOHProject.Application.Ports;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.Services.EntryPoints;
using MOHProject.Domain.ValueObjects;
using MOHProject.Infrastructure.Persistence;

namespace MOHProject.Tests.Integration.Application;

// End-to-end acceptance: UwDecisionCommand runs against real SQL Server through
// real EF-backed ports (repository, letter generator, audit writer, unit of work)
// and real pure domain services (evaluator, entry-point registry).
//
// Verifies:
//   - Substatus, letters, audit are all persisted from a single command call
//   - Rollback: throwing partway through leaves no persisted side effects
[Collection(nameof(SqlServerCollection))]
public class UwDecisionCommandE2ETests
{
    private readonly SqlServerFixture _fixture;

    public UwDecisionCommandE2ETests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ApsAtCondAccept_PersistsMedicalEvidenceLetter_UpdatesSubstatus_WritesAudit()
    {
        var policyId = await CreateCondAcceptPolicy();

        await using (var db = _fixture.CreateContext())
        {
            var sut = BuildSut(db);
            await sut.HandleAsync(new UwDecisionCommand(policyId, UwDecision.Aps, "u1"), default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var policy = await db.Policies.SingleAsync(p => p.Id == policyId);
            policy.Substatus.Should().Be(PolicySubstatus.PendingUwAps,
                "APS override on entry 1.5.1 → PendingUwAps");

            var letters = await db.Letters.Where(l => l.PolicyId == policyId).ToListAsync();
            letters.Should().ContainSingle().Which.Type.Should().Be(LetterType.MedicalEvidence);

            var audits = await db.AuditEntries.Where(a => a.PolicyId == policyId).ToListAsync();
            audits.Should().ContainSingle().Which.EventType.Should().Be(UwDecisionCommandHandler.AuditEventType);
            audits.Single().ActorUserId.Should().Be("u1");
        }
    }

    [Fact]
    public async Task StandardAtCondAccept_WithSubstandardRider_EmitsCloa_UpdatesUwState_AllInOneTransaction()
    {
        var policyId = await CreateCondAcceptPolicyWithSubstandardRider();

        await using (var db = _fixture.CreateContext())
        {
            var sut = BuildSut(db);
            await sut.HandleAsync(new UwDecisionCommand(policyId, UwDecision.Standard, "u2"), default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var policy = await db.Policies.Include(p => p.UWState).SingleAsync(p => p.Id == policyId);
            policy.Substatus.Should().Be(PolicySubstatus.ConditionalAcceptanceLetterGenerated,
                "ExclusionOnly composition + AcceptCloa=Blank → evaluator holds at CondAccept");
            policy.UWState!.RcmpFlag.Should().BeFalse();
            policy.UWState.AcceptCloaEnabled.Should().BeFalse(
                "ExclusionOnly + AcceptCloa=Blank → AcceptCloa disabled (only enabled when Yes)");

            var letters = await db.Letters.Where(l => l.PolicyId == policyId).ToListAsync();
            letters.Should().ContainSingle().Which.Type.Should().Be(LetterType.CloaExclusion);

            var audits = await db.AuditEntries.Where(a => a.PolicyId == policyId).ToListAsync();
            audits.Should().ContainSingle();
        }
    }

    [Fact]
    public async Task Transaction_RollsBack_WhenAuditWriterThrows_NoLetterOrSubstatusPersisted()
    {
        var policyId = await CreateCondAcceptPolicy();
        Guid throwSentinel = Guid.NewGuid();

        await using (var db = _fixture.CreateContext())
        {
            var sut = BuildSut(db, throwingAuditGuid: throwSentinel);

            Func<Task> act = () => sut.HandleAsync(new UwDecisionCommand(policyId, UwDecision.Aps, "u3"), default);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .Where(e => e.Message.Contains(throwSentinel.ToString()));
        }

        await using (var db = _fixture.CreateContext())
        {
            var policy = await db.Policies.SingleAsync(p => p.Id == policyId);
            policy.Substatus.Should().Be(PolicySubstatus.ConditionalAcceptanceLetterGenerated,
                "audit writer threw → transaction rolled back → substatus unchanged from pre-command state");

            var letters = await db.Letters.Where(l => l.PolicyId == policyId).ToListAsync();
            letters.Should().BeEmpty(
                "letter was generated inside the transaction — rollback must remove it, otherwise letters and audit drift");

            var audits = await db.AuditEntries.Where(a => a.PolicyId == policyId).ToListAsync();
            audits.Should().BeEmpty();
        }
    }

    // ------------------------------------------------------------------

    private UwDecisionCommandHandler BuildSut(AppDbContext db, Guid? throwingAuditGuid = null)
    {
        IPolicyRepository repo = new EfPolicyRepository(db);
        IAuditTrailWriter audit = throwingAuditGuid is { } sentinel
            ? new ThrowingAudit(sentinel)
            : new EfAuditTrailWriter(db);
        ILetterGenerator letters = new EfLetterGenerator(db);
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

        return new UwDecisionCommandHandler(repo, registry, evaluator, letters, audit, uow);
    }

    private async Task<long> CreateCondAcceptPolicy()
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
            UWState = new UWState { AcceptCloa = AcceptCloa.Yes },
            PremiumCollection = new PremiumCollection(),
        };
        policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active, GrossPremium = new Money(1000m) });
        db.Policies.Add(policy);
        await db.SaveChangesAsync();
        return policy.Id;
    }

    private async Task<long> CreateCondAcceptPolicyWithSubstandardRider()
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
            PremiumCollection = new PremiumCollection(),
        };
        policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active });
        policy.Plans.Add(new Plan { IsBase = false, ProductCode = "Rider", Status = ProductStatus.Active, HasActiveExclusion = true });
        db.Policies.Add(policy);
        await db.SaveChangesAsync();
        return policy.Id;
    }

    // Deliberately throws when WriteAsync is called — asserts transaction rollback.
    private sealed class ThrowingAudit : IAuditTrailWriter
    {
        private readonly Guid _sentinel;
        public ThrowingAudit(Guid sentinel) => _sentinel = sentinel;

        public Task WriteAsync(long policyId, string eventType, string actorUserId, object payload, CancellationToken ct)
            => throw new InvalidOperationException($"Simulated audit failure ({_sentinel})");
    }
}
