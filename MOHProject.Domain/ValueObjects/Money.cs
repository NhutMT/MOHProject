namespace MOHProject.Domain.ValueObjects;

public readonly record struct Money : IComparable<Money>
{
    public const string DefaultCurrency = "SGD";

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator -(Money value) => new(-value.Amount, value.Currency);

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount > right.Amount;
    }

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount < right.Amount;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount <= right.Amount;
    }

    public static Money Max(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.Amount >= right.Amount ? left : right;
    }

    public int CompareTo(Money other)
    {
        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public bool IsPositive => Amount > 0m;
    public bool IsNegative => Amount < 0m;
    public bool IsZero => Amount == 0m;

    public override string ToString() =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.00} {1}", Amount, Currency);

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Cannot operate on Money values of different currencies: '{left.Currency}' vs '{right.Currency}'.");
    }
}
