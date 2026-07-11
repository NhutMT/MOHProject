using MOHProject.Domain.ValueObjects;

namespace MOHProject.Domain.Entities;

public class PremiumCollection
{
    public long Id { get; set; }

    public Money BaseToCollect { get; set; }
    public Money BaseCollected { get; set; }

    public Money LinkedRidersToCollect { get; set; }
    public Money LinkedRidersCollected { get; set; }

    public Money UnallocatedCash { get; set; }

    public Money BaseShortfall => Money.Max(Money.Zero(BaseToCollect.Currency), BaseToCollect - BaseCollected);
    public Money LinkedRidersShortfall => Money.Max(Money.Zero(LinkedRidersToCollect.Currency), LinkedRidersToCollect - LinkedRidersCollected);
    public Money TotalShortfall => BaseShortfall + LinkedRidersShortfall;
}
