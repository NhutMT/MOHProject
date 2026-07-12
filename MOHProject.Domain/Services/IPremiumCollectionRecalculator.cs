using MOHProject.Domain.Entities;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Domain.Services;

public interface IPremiumCollectionRecalculator
{
    RecalcResult Recalculate(Policy policy);
}

// Signals whether Phase 4 caller should emit a Refund-of-Excess-Premium letter.
// The excess amount is what moved from the "collected but no longer needed"
// bucket into UnallocatedCash on this call.
public sealed record RecalcResult(Money BaseExcess, Money LinkedRidersExcess)
{
    public Money TotalExcess => BaseExcess + LinkedRidersExcess;
    public bool RefundLetterRequired => TotalExcess.IsPositive;
}
