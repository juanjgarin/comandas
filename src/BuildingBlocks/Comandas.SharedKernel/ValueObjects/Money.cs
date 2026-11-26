using System.Globalization;
using Comandas.SharedKernel.Domain;

namespace Comandas.SharedKernel.ValueObjects;

/// <summary>
/// An amount of money in a given currency (Value Object).
/// Rules: never negative, always has an ISO 4217 currency code, amounts are kept at 2 decimal places,
/// and arithmetic between different currencies is not allowed.
/// Immutable: every operation returns a new instance.
/// </summary>
public sealed class Money : ValueObject
{
    public const int DecimalPlaces = 2;

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    /// <summary>ISO 4217 code in upper case, e.g. "EUR".</summary>
    public string Currency { get; }

    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new DomainException($"Money amount cannot be negative (was {amount.ToString(CultureInfo.InvariantCulture)}).");
        }

        return new Money(Round(amount), NormalizeCurrency(currency));
    }

    public static Money Zero(string currency) => Of(0m, currency);

    public static Money operator +(Money left, Money right) => Add(left, right);

    public static Money operator -(Money left, Money right) => Subtract(left, right);

    public static Money operator *(Money money, decimal factor) => Multiply(money, factor);

    public static Money Add(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money Subtract(Money left, Money right)
    {
        EnsureSameCurrency(left, right);

        if (right.Amount > left.Amount)
        {
            throw new DomainException($"Cannot subtract {right} from {left}: the result would be negative.");
        }

        return new Money(left.Amount - right.Amount, left.Currency);
    }

    /// <summary>Multiplies by a non-negative factor (a quantity, or a percentage such as 0.15m).</summary>
    public static Money Multiply(Money money, decimal factor)
    {
        ArgumentNullException.ThrowIfNull(money);

        if (factor < 0)
        {
            throw new DomainException("Money cannot be multiplied by a negative factor.");
        }

        return new Money(Round(money.Amount * factor), money.Currency);
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Amount:0.00} {Currency}");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    // "Round half away from zero" is what people expect from prices (2.345 -> 2.35), unlike the
    // banker's rounding that decimal.Round uses by default (2.345 -> 2.34).
    private static decimal Round(decimal amount) =>
        decimal.Round(amount, DecimalPlaces, MidpointRounding.AwayFromZero);

    private static string NormalizeCurrency(string currency)
    {
        var normalized = currency?.Trim().ToUpperInvariant();

        if (normalized is not { Length: 3 } || !normalized.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException($"'{currency}' is not a valid ISO 4217 currency code (expected 3 letters, e.g. EUR).");
        }

        return normalized;
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Currency != right.Currency)
        {
            throw new DomainException($"Cannot combine {left.Currency} with {right.Currency}.");
        }
    }
}
