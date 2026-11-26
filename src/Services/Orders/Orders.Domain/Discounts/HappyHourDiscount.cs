using System.Globalization;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Discounts;

/// <summary>
/// A percentage off the whole order, only inside a daily time window, e.g. 20% from 18:00 to 20:00
/// (a concrete Strategy).
/// </summary>
/// <remarks>
/// <para>
/// Time is a DEPENDENCY (Dependency Inversion): the current time comes from an injected
/// <see cref="TimeProvider"/>, never from <c>DateTime.Now</c>, so tests can set the clock to any instant.
/// </para>
/// <para>
/// Composition over inheritance: the calculation itself is delegated to a <see cref="PercentageDiscount"/>;
/// this class only adds the "when".
/// </para>
/// <para>
/// The window is evaluated in the server's local time when the discount is calculated. In a Linux container
/// local time is UTC unless the TZ environment variable is set (see phase 3).
/// </para>
/// </remarks>
public sealed class HappyHourDiscount : IDiscountStrategy
{
    private readonly TimeProvider _clock;
    private readonly PercentageDiscount _percentage;

    public HappyHourDiscount(TimeProvider clock, TimeOnly start, TimeOnly end, decimal percentage)
    {
        ArgumentNullException.ThrowIfNull(clock);

        if (start == end)
        {
            throw new DomainException("Happy hour must start and end at different times.");
        }

        _clock = clock;
        _percentage = new PercentageDiscount(percentage); // validates the percentage
        Start = start;
        End = end;
    }

    /// <summary>First minute of the window (included).</summary>
    public TimeOnly Start { get; }

    /// <summary>End of the window (excluded): at exactly <see cref="End"/> happy hour is over.</summary>
    public TimeOnly End { get; }

    public decimal Percentage => _percentage.Percentage;

    public string Description =>
        string.Create(CultureInfo.InvariantCulture, $"Happy hour {Percentage:0.##}% off ({Start:HH\\:mm}-{End:HH\\:mm})");

    /// <summary>
    /// Whether "now" is inside the window. <see cref="TimeOnly.IsBetween"/> includes the start, excludes the end
    /// and supports windows that cross midnight (e.g. 23:00-01:00).
    /// </summary>
    public bool IsActive()
    {
        var now = TimeOnly.FromDateTime(_clock.GetLocalNow().DateTime);
        return now.IsBetween(Start, End);
    }

    public Money CalculateDiscount(DiscountContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return IsActive()
            ? _percentage.CalculateDiscount(context)
            : Money.Zero(context.Subtotal.Currency);
    }
}
