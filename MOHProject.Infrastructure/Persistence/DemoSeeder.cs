using Microsoft.EntityFrameworkCore;
using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Infrastructure.Persistence;

// Demo-only. Seeds one policy from BUG-2610000310P (post-fix state) so the
// /policies/{policyNumber} endpoint returns something meaningful without
// exposing a full CRUD surface yet. Idempotent — checks first.
public static class DemoSeeder
{
    public const string DemoPolicyNumber = "2610000310P";

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Policies.AnyAsync(p => p.PolicyNumber == DemoPolicyNumber, ct))
            return;

        var insured = new Insured
        {
            Residency = Residency.Sg,
            DateOfBirth = new DateTime(1990, 3, 14),
            ExternalId = "INS-DEMO-01",
        };
        var payer = new Payer
        {
            Residency = Residency.Sg,
            DateOfBirth = new DateTime(1965, 8, 2),
            ExternalId = "PAY-DEMO-01",
        };
        var policyHolder = new PolicyHolder
        {
            Residency = Residency.Sg,
            DateOfBirth = new DateTime(1965, 8, 2),
            ExternalId = "PH-DEMO-01",
        };

        var uwState = new UWState
        {
            RcmpFlag = false,
            RcmpFlagEnabled = false,
            AcceptCloa = AcceptCloa.Blank,
            AcceptCloaEnabled = false,
            RcmpOption = RcmpOption.Blank,
            RcmpOptionEnabled = false,
            CompleteUw = true,
            CurrentComposition = RiskComposition.AllStandard,
        };
        var premiumCollection = new PremiumCollection
        {
            BaseToCollect = new Money(1200m),
            BaseCollected = new Money(1200m),
            LinkedRidersToCollect = new Money(480m),
            LinkedRidersCollected = new Money(230m),
            UnallocatedCash = Money.Zero(),
        };

        var policy = new Policy
        {
            PolicyNumber = DemoPolicyNumber,
            Type = PolicyType.NewBusiness,
            Substatus = PolicySubstatus.PendingCashCollection,
            InsuredResidency = Residency.Sg,
            PayerResidency = Residency.Sg,
            UwCompletedAt = DateTime.UtcNow.AddDays(-1),
            Insured = insured,
            Payer = payer,
            PolicyHolder = policyHolder,
            UWState = uwState,
            PremiumCollection = premiumCollection,
        };

        policy.Plans.Add(new Plan
        {
            IsBase = true,
            ProductCode = "Base",
            Status = ProductStatus.Active,
            HasActiveRiskLoading = false,
            HasActiveExclusion = false,
            GrossPremium = new Money(1200m),
            PrivateInsuranceExtraPremium = Money.Zero(),
            AddedAt = DateTime.UtcNow.AddDays(-30),
        });
        policy.Plans.Add(new Plan
        {
            IsBase = false,
            ProductCode = "CancerGuard",
            Status = ProductStatus.Active,
            HasActiveRiskLoading = false,
            HasActiveExclusion = false,
            GrossPremium = new Money(230m),
            PrivateInsuranceExtraPremium = Money.Zero(),
            AddedAt = DateTime.UtcNow.AddDays(-5),
        });
        policy.Plans.Add(new Plan
        {
            IsBase = false,
            ProductCode = "Choice",
            Status = ProductStatus.Declined,
            HasActiveRiskLoading = false,
            HasActiveExclusion = false,
            GrossPremium = new Money(250m),
            PrivateInsuranceExtraPremium = Money.Zero(),
            AddedAt = DateTime.UtcNow.AddDays(-5),
            StatusChangedAt = DateTime.UtcNow.AddDays(-1),
        });
        policy.Plans.Add(new Plan
        {
            IsBase = false,
            ProductCode = "Premier",
            Status = ProductStatus.NotTakenUp,
            HasActiveRiskLoading = false,
            HasActiveExclusion = true,
            GrossPremium = new Money(300m),
            PrivateInsuranceExtraPremium = new Money(300m),
            AddedAt = DateTime.UtcNow.AddDays(-5),
            StatusChangedAt = DateTime.UtcNow.AddDays(-1),
        });

        policy.AuditEntries.Add(new AuditEntry
        {
            OccurredAt = DateTime.UtcNow.AddDays(-1),
            ActorUserId = "seed",
            EventType = "RiderStatusChanged",
            PayloadJson = """{"riderProductCode":"Premier","previousStatus":"Active","newStatus":"NotTakenUp","note":"BUG-2610000310P setup — Premier NTU'd; evaluator moved substatus to PendingCashCollection"}""",
        });

        db.Policies.Add(policy);
        await db.SaveChangesAsync(ct);
    }
}
