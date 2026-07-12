using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Domain.Services;

// Implements FR-AOR-030 step 1 (source lines 662-687).
// Recomputes ToCollect from Active plans; keeps Collected unchanged
// (doc: "Collected: Giữ nguyên"); moves any excess into UnallocatedCash
// and signals a Refund-of-Excess-Premium letter.
//
// Fix for the doc-reported bug: Shortfall must never serialize as negative.
// The `PremiumCollection.*Shortfall` getters already clamp to Money.Zero,
// so this recalculator focuses on the excess-detection side.
public sealed class PremiumCollectionRecalculator : IPremiumCollectionRecalculator
{
    public RecalcResult Recalculate(Policy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (policy.PremiumCollection is null)
            throw new InvalidOperationException($"Policy {policy.Id} has no PremiumCollection.");

        var pc = policy.PremiumCollection;

        var basePlan = policy.Plans.FirstOrDefault(p => p.IsBase && p.Status == ProductStatus.Active);
        var activeRiders = policy.Plans.Where(p => !p.IsBase && p.Status == ProductStatus.Active).ToArray();

        var newBaseToCollect = basePlan?.GrossPremium ?? Money.Zero(pc.BaseToCollect.Currency);
        var newLinkedRidersToCollect = activeRiders.Aggregate(
            Money.Zero(pc.LinkedRidersToCollect.Currency),
            (acc, plan) => acc + plan.GrossPremium);

        // Excess = Collected - newToCollect, floored at 0.
        var baseExcess = Money.Max(Money.Zero(pc.BaseToCollect.Currency), pc.BaseCollected - newBaseToCollect);
        var linkedRidersExcess = Money.Max(Money.Zero(pc.LinkedRidersToCollect.Currency), pc.LinkedRidersCollected - newLinkedRidersToCollect);

        pc.BaseToCollect = newBaseToCollect;
        pc.LinkedRidersToCollect = newLinkedRidersToCollect;

        // Move any newly-detected excess into UnallocatedCash.
        // Collected stays as-is per doc — UnallocatedCash is a separate
        // accounting bucket that Finance draws down when processing refunds.
        var totalExcess = baseExcess + linkedRidersExcess;
        if (totalExcess.IsPositive)
            pc.UnallocatedCash = pc.UnallocatedCash + totalExcess;

        return new RecalcResult(baseExcess, linkedRidersExcess);
    }
}
