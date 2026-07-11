using MOHProject.Domain.ValueObjects;

namespace MOHProject.Tests.Unit.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Zero_DefaultCurrency_Is_SGD_And_Amount_Zero()
    {
        var zero = Money.Zero();
        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("SGD");
        zero.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Constructor_EmptyCurrency_Throws()
    {
        Action act = () => _ = new Money(1m, "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_SameCurrency_ReturnsSum()
    {
        var result = new Money(10m) + new Money(2.5m);
        result.Amount.Should().Be(12.5m);
        result.Currency.Should().Be("SGD");
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws()
    {
        Action act = () => _ = new Money(1m, "SGD") + new Money(1m, "USD");
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SGD*USD*");
    }

    [Fact]
    public void Subtract_SameCurrency_ReturnsDifference()
    {
        var result = new Money(10m) - new Money(3m);
        result.Amount.Should().Be(7m);
    }

    [Fact]
    public void Subtract_ProducesNegative_Allowed()
    {
        var result = new Money(1m) - new Money(2m);
        result.IsNegative.Should().BeTrue();
        result.Amount.Should().Be(-1m);
    }

    [Fact]
    public void UnaryNegate_FlipsSign()
    {
        var negated = -new Money(5m);
        negated.Amount.Should().Be(-5m);
    }

    [Fact]
    public void Max_ReturnsLarger()
    {
        Money.Max(new Money(3m), new Money(5m)).Amount.Should().Be(5m);
        Money.Max(new Money(-1m), new Money(0m)).Amount.Should().Be(0m);
    }

    [Fact]
    public void Max_DifferentCurrencies_Throws()
    {
        Action act = () => Money.Max(new Money(1m, "SGD"), new Money(1m, "USD"));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Comparison_Operators_WorkForSameCurrency()
    {
        (new Money(10m) > new Money(5m)).Should().BeTrue();
        (new Money(5m) < new Money(10m)).Should().BeTrue();
        (new Money(5m) >= new Money(5m)).Should().BeTrue();
        (new Money(5m) <= new Money(5m)).Should().BeTrue();
    }

    [Fact]
    public void Equality_UsesRecordValueSemantics()
    {
        var a = new Money(1.23m, "SGD");
        var b = new Money(1.23m, "SGD");
        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentCurrency_NotEqual()
    {
        var a = new Money(1m, "SGD");
        var b = new Money(1m, "USD");
        (a == b).Should().BeFalse();
    }

    [Fact]
    public void CompareTo_SameCurrency_OrdersCorrectly()
    {
        var items = new[] { new Money(3m), new Money(1m), new Money(2m) };
        Array.Sort(items);
        items.Select(m => m.Amount).Should().ContainInOrder(1m, 2m, 3m);
    }

    [Fact]
    public void ToString_ShowsAmountAndCurrency()
    {
        new Money(1234.5m).ToString().Should().Be("1234.50 SGD");
    }

    [Fact]
    public void IsPositive_IsNegative_IsZero_AreExclusive()
    {
        new Money(1m).IsPositive.Should().BeTrue();
        new Money(-1m).IsNegative.Should().BeTrue();
        new Money(0m).IsZero.Should().BeTrue();
    }
}
