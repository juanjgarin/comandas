using System.Globalization;
using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Orders;

/// <summary>
/// How many units of a menu item an order line contains (Value Object).
/// Replaces a raw <c>int</c>: if you hold a <see cref="Quantity"/>, it is valid (between 1 and 99).
/// </summary>
public sealed class Quantity : ValueObject
{
    public const int Min = 1;

    /// <summary>Upper bound per order line: protects against typos such as 1000 coffees.</summary>
    public const int Max = 99;

    private Quantity(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static Quantity One { get; } = new(Min);

    public static Quantity Of(int value)
    {
        if (value is < Min or > Max)
        {
            throw new DomainException($"Quantity must be between {Min} and {Max} (was {value}).");
        }

        return new Quantity(value);
    }

    public static Quantity operator +(Quantity left, Quantity right) => Add(left, right);

    /// <summary>Combines two quantities (e.g. the same dish added twice). The result must still be within bounds.</summary>
    public static Quantity Add(Quantity left, Quantity right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return Of(left.Value + right.Value);
    }

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
