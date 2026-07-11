using MOHProject.Domain.Entities;
using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Unit.Domain.Entities;

public class PremiumCollectionDefaultsTests
{
    [Fact]
    public void NewInstance_MoneyPropertiesInitializedToZero_NotDefaultStruct()
    {
        var pc = new PremiumCollection();

        pc.BaseToCollect.Currency.Should().Be("SGD",
            "Money default struct has null Currency; initializer must set SGD");
        pc.BaseCollected.Currency.Should().Be("SGD");
        pc.LinkedRidersToCollect.Currency.Should().Be("SGD");
        pc.LinkedRidersCollected.Currency.Should().Be("SGD");
        pc.UnallocatedCash.Currency.Should().Be("SGD");
    }

    [Fact]
    public void NewInstance_ShortfallAccessors_DoNotThrow()
    {
        var pc = new PremiumCollection();

        Action baseShortfall = () => _ = pc.BaseShortfall;
        Action linkedShortfall = () => _ = pc.LinkedRidersShortfall;
        Action totalShortfall = () => _ = pc.TotalShortfall;

        baseShortfall.Should().NotThrow(
            "regression: uninitialized Money had null Currency, causing Money.Zero(null) to throw in the shortfall accessor");
        linkedShortfall.Should().NotThrow();
        totalShortfall.Should().NotThrow();

        pc.BaseShortfall.Should().Be(Money.Zero());
        pc.LinkedRidersShortfall.Should().Be(Money.Zero());
        pc.TotalShortfall.Should().Be(Money.Zero());
    }

    [Fact]
    public void Plan_NewInstance_MoneyPropertiesInitializedToZero()
    {
        var plan = new Plan();

        plan.GrossPremium.Currency.Should().Be("SGD");
        plan.PrivateInsuranceExtraPremium.Currency.Should().Be("SGD");
    }
}
