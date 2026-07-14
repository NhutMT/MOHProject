using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.ValueObjects;
using MOHProject.Infrastructure.Persistence;
using MOHProject.Infrastructure.Reminders;

namespace MOHProject.Tests.Integration.Infrastructure;

[Collection(nameof(SqlServerCollection))]
public class EfLetterGeneratorTests
{
    private readonly SqlServerFixture _fixture;

    public EfLetterGeneratorTests(SqlServerFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Generate_InsertsLetter_WithFreshCorrelationId_AndIsCurrentTrue()
    {
        var policyId = await CreatePolicy();

        Letter emitted;
        await using (var db = _fixture.CreateContext())
        {
            var sut = NewLetterGenerator(db);
            emitted = await sut.GenerateAsync(policyId, LetterType.Loa, default);
        }

        emitted.IsCurrent.Should().BeTrue();
        emitted.CorrelationId.Should().NotBe(Guid.Empty);

        await using (var db = _fixture.CreateContext())
        {
            var persisted = await db.Letters.SingleAsync(l => l.Id == emitted.Id);
            persisted.Type.Should().Be(LetterType.Loa);
            persisted.PolicyId.Should().Be(policyId);
            persisted.IsCurrent.Should().BeTrue();
            persisted.IssuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task Generate_TwoLoasInSequence_MarksFirstNotCurrent_KeepsSecondCurrent()
    {
        var policyId = await CreatePolicy();

        Letter v1, v2;
        await using (var db = _fixture.CreateContext())
        {
            var sut = NewLetterGenerator(db);
            v1 = await sut.GenerateAsync(policyId, LetterType.Loa, default);
        }
        await using (var db = _fixture.CreateContext())
        {
            var sut = NewLetterGenerator(db);
            v2 = await sut.GenerateAsync(policyId, LetterType.Loa, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var loas = await db.Letters
                .Where(l => l.PolicyId == policyId && l.Type == LetterType.Loa)
                .ToListAsync();

            loas.Should().HaveCount(2);
            loas.Single(l => l.Id == v1.Id).IsCurrent.Should().BeFalse(
                "supersession marks the previous same-type letter as not current");
            loas.Single(l => l.Id == v2.Id).IsCurrent.Should().BeTrue();

            v1.CorrelationId.Should().NotBe(v2.CorrelationId,
                "each generated letter gets a fresh CorrelationId — reminder cancellation keys off this");
        }
    }

    [Fact]
    public async Task Generate_LoaSupersedes_DoesNotAffectCloa()
    {
        var policyId = await CreatePolicy();

        await using (var db = _fixture.CreateContext())
        {
            var sut = NewLetterGenerator(db);
            await sut.GenerateAsync(policyId, LetterType.Loa, default);
            await sut.GenerateAsync(policyId, LetterType.CloaExclusion, default);
            // A new LOA should supersede the earlier LOA but leave the CLOA intact.
            await sut.GenerateAsync(policyId, LetterType.Loa, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var loas = await db.Letters.Where(l => l.PolicyId == policyId && l.Type == LetterType.Loa).ToListAsync();
            var cloas = await db.Letters.Where(l => l.PolicyId == policyId && l.Type == LetterType.CloaExclusion).ToListAsync();

            loas.Should().HaveCount(2);
            loas.Count(l => l.IsCurrent).Should().Be(1, "only the newest LOA is current");
            cloas.Should().ContainSingle().Which.IsCurrent.Should().BeTrue(
                "superseding an LOA must not touch a CLOA of a different type");
        }
    }

    [Fact]
    public async Task Generate_Loa_IncludesOnlyActivePlans_ExcludesNtuDeclinedPostponed()
    {
        var policyId = await CreatePolicyWithMixedPlans();

        Letter loa;
        await using (var db = _fixture.CreateContext())
        {
            loa = await NewLetterGenerator(db).GenerateAsync(policyId, LetterType.Loa, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var included = await db.LetterPlans
                .Where(lp => lp.LetterId == loa.Id)
                .Include(lp => lp.Plan)
                .Select(lp => new { lp.Plan!.ProductCode, lp.Plan.Status })
                .ToListAsync();

            included.Should().OnlyContain(x => x.Status == ProductStatus.Active,
                "FR-LTR-005 / FR-AOR-041: LOA includes only Active plans");
            included.Select(x => x.ProductCode).Should().BeEquivalentTo(new[] { "Base", "CancerGuard" });
        }
    }

    [Fact]
    public async Task Generate_NtuLetter_IncludesOnlyRidersNtudInCurrentCycle()
    {
        // Setup: previous cycle NTU'd Choice one week ago. Current cycle just NTU'd Premier now.
        // UwCompletedAt = 2 days ago (start of current cycle = end of previous cycle).
        // Expected: NTU letter includes only Premier (Choice was excluded per PH-04-08).
        var policyNumber = $"P-{Guid.NewGuid():N}"[..30];
        long policyId;

        await using (var db = _fixture.CreateContext())
        {
            var uwCompletedAt = DateTime.UtcNow.AddDays(-2);
            var policy = new Policy
            {
                PolicyNumber = policyNumber,
                Type = PolicyType.NewBusiness,
                Substatus = PolicySubstatus.ConditionalAcceptanceLetterGenerated,
                InsuredResidency = Residency.Sg,
                PayerResidency = Residency.Sg,
                UwCompletedAt = uwCompletedAt,
            };
            policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active, AddedAt = DateTime.UtcNow.AddDays(-30) });
            policy.Plans.Add(new Plan
            {
                IsBase = false, ProductCode = "Choice", Status = ProductStatus.NotTakenUp,
                AddedAt = DateTime.UtcNow.AddDays(-10),
                StatusChangedAt = DateTime.UtcNow.AddDays(-7), // BEFORE UwCompletedAt → previous cycle
            });
            policy.Plans.Add(new Plan
            {
                IsBase = false, ProductCode = "Premier", Status = ProductStatus.NotTakenUp,
                AddedAt = DateTime.UtcNow.AddDays(-3),
                StatusChangedAt = DateTime.UtcNow, // in current cycle (>= UwCompletedAt)
            });
            db.Policies.Add(policy);
            await db.SaveChangesAsync();
            policyId = policy.Id;
        }

        Letter ntu;
        await using (var db = _fixture.CreateContext())
        {
            ntu = await NewLetterGenerator(db).GenerateAsync(policyId, LetterType.NtuWithoutRefund, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var included = await db.LetterPlans
                .Where(lp => lp.LetterId == ntu.Id)
                .Include(lp => lp.Plan)
                .Select(lp => lp.Plan!.ProductCode)
                .ToListAsync();

            included.Should().BeEquivalentTo(new[] { "Premier" },
                "PH-04-08: Choice was NTU'd BEFORE current UwCompletedAt → excluded; " +
                "Premier NTU'd in current cycle → included");
        }
    }

    [Fact]
    public async Task Generate_MedicalEvidence_IncludesOnlyActivePlans()
    {
        var policyId = await CreatePolicyWithMixedPlans();

        Letter letter;
        await using (var db = _fixture.CreateContext())
        {
            letter = await NewLetterGenerator(db).GenerateAsync(policyId, LetterType.MedicalEvidence, default);
        }

        await using (var db = _fixture.CreateContext())
        {
            var included = await db.LetterPlans.CountAsync(lp => lp.LetterId == letter.Id);
            included.Should().Be(2, "Base + CancerGuard are Active; NTU'd Premier and Declined Choice are excluded");
        }
    }

    [Fact]
    public async Task Generate_UnknownType_Throws()
    {
        var policyId = await CreatePolicy();

        await using var db = _fixture.CreateContext();
        var sut = NewLetterGenerator(db);

        Func<Task> act = () => sut.GenerateAsync(policyId, (LetterType)999, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Unhandled letter type*");
    }

    [Fact]
    public async Task Generate_PolicyNotFound_Throws()
    {
        await using var db = _fixture.CreateContext();
        var sut = NewLetterGenerator(db);

        Func<Task> act = () => sut.GenerateAsync(999_999_999, LetterType.Loa, default);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*999999999*not found*");
    }

    private static EfLetterGenerator NewLetterGenerator(AppDbContext db) =>
        new(db, new EfReminderScheduler(db, Options.Create(new ReminderSchedulingOptions())));

    private async Task<long> CreatePolicy()
    {
        var policyNumber = $"P-{Guid.NewGuid():N}"[..30];
        await using var db = _fixture.CreateContext();
        var policy = new Policy
        {
            PolicyNumber = policyNumber,
            Type = PolicyType.NewBusiness,
            Substatus = PolicySubstatus.PendingManualUnderwriting,
            InsuredResidency = Residency.Sg,
            PayerResidency = Residency.Sg,
        };
        db.Policies.Add(policy);
        await db.SaveChangesAsync();
        return policy.Id;
    }

    private async Task<long> CreatePolicyWithMixedPlans()
    {
        var policyNumber = $"P-{Guid.NewGuid():N}"[..30];
        await using var db = _fixture.CreateContext();
        var policy = new Policy
        {
            PolicyNumber = policyNumber,
            Type = PolicyType.NewBusiness,
            Substatus = PolicySubstatus.PendingCashCollection,
            InsuredResidency = Residency.Sg,
            PayerResidency = Residency.Sg,
        };
        policy.Plans.Add(new Plan { IsBase = true, ProductCode = "Base", Status = ProductStatus.Active });
        policy.Plans.Add(new Plan { IsBase = false, ProductCode = "CancerGuard", Status = ProductStatus.Active });
        policy.Plans.Add(new Plan { IsBase = false, ProductCode = "Choice", Status = ProductStatus.Declined });
        policy.Plans.Add(new Plan { IsBase = false, ProductCode = "Premier", Status = ProductStatus.NotTakenUp });
        db.Policies.Add(policy);
        await db.SaveChangesAsync();
        return policy.Id;
    }
}
