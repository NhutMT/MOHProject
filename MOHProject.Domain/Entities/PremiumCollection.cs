using MOHProject.Domain.ValueObjects;

namespace MOHProject.Domain.Entities;

public class PremiumCollection
{
    public long Id { get; set; }

    public Money BaseToCollect { get; set; } = Money.Zero();
    public Money BaseCollected { get; set; } = Money.Zero();

    public Money LinkedRidersToCollect { get; set; } = Money.Zero();
    public Money LinkedRidersCollected { get; set; } = Money.Zero();

    public Money UnallocatedCash { get; set; } = Money.Zero();

    public Money BaseShortfall => Money.Max(Money.Zero(BaseToCollect.Currency), BaseToCollect - BaseCollected);
    public Money LinkedRidersShortfall => Money.Max(Money.Zero(LinkedRidersToCollect.Currency), LinkedRidersToCollect - LinkedRidersCollected);
    public Money TotalShortfall => BaseShortfall + LinkedRidersShortfall;
}
