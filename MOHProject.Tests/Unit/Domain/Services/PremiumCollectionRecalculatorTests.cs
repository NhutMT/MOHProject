using MOHProject.Domain.Entities;
using MOHProject.Domain.Enums;
using MOHProject.Domain.Services;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Unit.Domain.Services;

public class PremiumCollectionRecalculatorTests
{
    private readonly PremiumCollectionRecalculator _sut = new();

    [Fact]
    public void NullPolicy_Throws()
    {
        Action act = () => _sut.Recalculate(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PolicyWithoutPremiumCollection_Throws()
    {
        var policy = new Policy { Id = 7 };
        Action act = () => _sut.Recalculate(policy);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Policy 7*PremiumCollection*");
    }

    [Fact]
    public void NoExcess_ShortfallEqualsDiff_NoUnallocatedCashChange()
    {
        var policy = PolicyWith(baseToCollect: 1000m, baseCollected: 700m,
                                riders: new[] { (250m, 200m) });

        var result = _sut.Recalculate(policy);

        result.RefundLetterRequired.Should().BeFalse();
        result.TotalExcess.Should().Be(Money.Zero());
        policy.PremiumCollection!.BaseShortfall.Should().Be(new Money(300m));
        policy.PremiumCollection.LinkedRidersShortfall.Should().Be(new Money(50m));
        policy.PremiumCollection.UnallocatedCash.Should().Be(Money.Zero(),
            "no excess → UnallocatedCash unchanged");
    }

    [Fact]
    public void SmallExcess_OnLinkedRiders_MovesToUnallocatedCash_TriggersRefund()
    {
        // Rider NTU'd → LinkedRidersToCollect drops from 480 to 230.
        // Collected stayed at 480 → excess = 250.
        var policy = PolicyWith(baseToCollect: 1200m, baseCollected: 1200m,
                                riders: new[] { (230m, 480m) });

        var result = _sut.Recalculate(policy);

        result.RefundLetterRequired.Should().BeTrue();
        result.LinkedRidersExcess.Should().Be(new Money(250m));
        result.BaseExcess.Should().Be(Money.Zero());
        policy.PremiumCollection!.UnallocatedCash.Should().Be(new Money(250m),
            "excess moves into UnallocatedCash bucket for Finance to process");
        policy.PremiumCollection.LinkedRidersShortfall.Should().Be(Money.Zero(),
            "Shortfall must never serialize as negative even when excess exists — fix for the doc-reported bug");
    }

    [Fact]
    public void LargeExcess_OnBothBaseAndLinkedRiders_Accumulates()
    {
        var policy = PolicyWith(baseToCollect: 800m, baseCollected: 1200m,
                                riders: new[] { (150m, 400m) });

        var result = _sut.Recalculate(policy);

        result.BaseExcess.Should().Be(new Money(400m), "base collected 1200 vs new to-collect 800 → excess 400");
        result.LinkedRidersExcess.Should().Be(new Money(250m), "linked collected 400 vs new to-collect 150 → excess 250");
        result.TotalExcess.Should().Be(new Money(650m));
        policy.PremiumCollection!.UnallocatedCash.Should().Be(new Money(650m));
    }

    [Fact]
    public void ZeroPremiumAfterAllRidersNtu_LinkedRidersToCollectZero_AllCollectedFlowsToUnallocated()
    {
        // Setup: all riders NTU'd. LinkedRidersToCollect should be 0.
        // Whatever was collected on riders becomes excess.
        var policy = new Policy
        {
            Id = 1,
            PremiumCollection = new PremiumCollection
            {
                BaseToCollect = new Money(1000m),
                BaseCollected = new Money(1000m),
                LinkedRidersToCollect = new Money(400m),
                LinkedRidersCollected = new Money(400m),
            },
        };
        policy.Plans.Add(BasePlan(1000m));
        policy.Plans.Add(new Plan { IsBase = false, Status = ProductStatus.NotTakenUp, GrossPremium = new Money(400m) });

        var result = _sut.Recalculate(policy);

        policy.PremiumCollection.LinkedRidersToCollect.Should().Be(Money.Zero(),
            "no Active rider → new LinkedRidersToCollect is zero");
        result.LinkedRidersExcess.Should().Be(new Money(400m));
        policy.PremiumCollection.UnallocatedCash.Should().Be(new Money(400m));
    }

    [Fact]
    public void ExcessAccumulates_AcrossMultipleRecalcCalls()
    {
        // Regression: excess is ADDED to UnallocatedCash, not replaced.
        // Two consecutive NTU events should both contribute.
        var policy = new Policy
        {
            Id = 1,
            PremiumCollection = new PremiumCollection
            {
                BaseToCollect = new Money(1000m),
                BaseCollected = new Money(1000m),
                LinkedRidersToCollect = new Money(300m),
                LinkedRidersCollected = new Money(300m),
                UnallocatedCash = new Money(50m), // pre-existing balance
            },
        };
        policy.Plans.Add(BasePlan(1000m));
        // Two riders — active initially, both stay active in this call.
        policy.Plans.Add(new Plan { IsBase = false, Status = ProductStatus.Active, GrossPremium = new Money(150m) });
        policy.Plans.Add(new Plan { IsBase = false, Status = ProductStatus.Active, GrossPremium = new Money(150m) });

        // First call: still balanced (2 × 150 = 300).
        _sut.Recalculate(policy);
        policy.PremiumCollection.UnallocatedCash.Should().Be(new Money(50m), "no new excess yet");

        // NTU one rider → excess 150.
        policy.Plans.Last().Status = ProductStatus.NotTakenUp;
        _sut.Recalculate(policy);

        policy.PremiumCollection.UnallocatedCash.Should().Be(new Money(200m),
            "second recalc adds 150 excess on top of the pre-existing 50 balance");
    }

    private static Policy PolicyWith(decimal baseToCollect, decimal baseCollected, (decimal GrossPremium, decimal _)[] riders)
    {
        var policy = new Policy
        {
            Id = 1,
            PremiumCollection = new PremiumCollection
            {
                BaseToCollect = new Money(baseToCollect),
                BaseCollected = new Money(baseCollected),
                LinkedRidersToCollect = new Money(riders.Sum(r => r.Item2)),
                LinkedRidersCollected = new Money(riders.Sum(r => r.Item2)),
            },
        };
        policy.Plans.Add(BasePlan(baseToCollect));
        foreach (var r in riders)
            policy.Plans.Add(new Plan { IsBase = false, Status = ProductStatus.Active, GrossPremium = new Money(r.GrossPremium) });
        return policy;
    }

    private static Plan BasePlan(decimal premium) => new()
    {
        IsBase = true,
        Status = ProductStatus.Active,
        GrossPremium = new Money(premium),
    };
}
