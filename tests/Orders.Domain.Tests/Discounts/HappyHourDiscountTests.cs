using Comandas.Orders.Domain.Discounts;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace Comandas.Orders.Domain.Tests.Discounts;

/// <summary>
/// Time-dependent rules tested deterministically: a FakeTimeProvider is set to exact instants instead of
/// depending on when the tests happen to run.
/// </summary>
public sealed class HappyHourDiscountTests
{
    private static readonly TimeOnly SixPm = new(18, 0);
    private static readonly TimeOnly EightPm = new(20, 0);

    [Theory]
    [InlineData("17:59:59", 0)]     // one second before: no discount
    [InlineData("18:00:00", 5.50)]  // start is INCLUDED
    [InlineData("19:30:00", 5.50)]
    [InlineData("19:59:59", 5.50)]
    [InlineData("20:00:00", 0)]     // end is EXCLUDED
    [InlineData("03:00:00", 0)]
    public void CalculateDiscount_AppliesOnlyInsideTheWindow(string localTime, decimal expected)
    {
        var clock = ClockAt(localTime);
        var happyHour = new HappyHourDiscount(clock, SixPm, EightPm, 20m);

        var amount = happyHour.CalculateDiscount(ContextWorth27Euros50());

        amount.Should().Be(Money.Of(expected, "EUR")); // 20% of 27.50 = 5.50
    }

    [Fact]
    public void CalculateDiscount_SameInstance_FollowsTheClockAsTimePasses()
    {
        var clock = ClockAt("19:59:59");
        var happyHour = new HappyHourDiscount(clock, SixPm, EightPm, 20m);
        var context = ContextWorth27Euros50();

        happyHour.CalculateDiscount(context).Should().Be(Money.Of(5.50m, "EUR"));

        clock.Advance(TimeSpan.FromSeconds(1)); // "time travel": now it is 20:00:00

        happyHour.CalculateDiscount(context).Should().Be(Money.Zero("EUR"));
    }

    [Theory]
    [InlineData("22:59:59", false)]
    [InlineData("23:00:00", true)]
    [InlineData("23:59:59", true)]
    [InlineData("00:00:00", true)]
    [InlineData("00:59:59", true)]
    [InlineData("01:00:00", false)]
    public void IsActive_WindowCrossingMidnight_WorksOnBothSides(string localTime, bool expected)
    {
        var lateNight = new HappyHourDiscount(ClockAt(localTime), new TimeOnly(23, 0), new TimeOnly(1, 0), 10m);

        lateNight.IsActive().Should().Be(expected);
    }

    [Fact]
    public void IsActive_UsesTheServersLocalTimeZone()
    {
        // 17:00 UTC is 19:00 in a UTC+2 zone: inside an 18:00-20:00 local window.
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 7, 1, 17, 0, 0, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.CreateCustomTimeZone("UTC+2", TimeSpan.FromHours(2), "UTC+2", "UTC+2"));

        new HappyHourDiscount(clock, SixPm, EightPm, 20m).IsActive().Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithSameStartAndEnd_ThrowsDomainException()
    {
        var act = () => new HappyHourDiscount(ClockAt("12:00:00"), SixPm, SixPm, 20m);

        act.Should().Throw<DomainException>().WithMessage("*different times*");
    }

    [Fact]
    public void Constructor_WithInvalidPercentage_ThrowsDomainException()
    {
        // Validation is reused from PercentageDiscount (composition), not duplicated.
        var act = () => new HappyHourDiscount(ClockAt("12:00:00"), SixPm, EightPm, 0m);

        act.Should().Throw<DomainException>().WithMessage("*percentage*");
    }

    [Fact]
    public void Description_ShowsPercentageAndWindow()
    {
        new HappyHourDiscount(ClockAt("12:00:00"), SixPm, EightPm, 20m)
            .Description.Should().Be("Happy hour 20% off (18:00-20:00)");
    }

    /// <summary>A fake clock whose LOCAL time is <paramref name="localTime"/> (local zone set to UTC for simplicity).</summary>
    private static FakeTimeProvider ClockAt(string localTime)
    {
        var time = TimeOnly.Parse(localTime, System.Globalization.CultureInfo.InvariantCulture);
        var clock = new FakeTimeProvider(new DateTimeOffset(new DateOnly(2026, 11, 26), time, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        return clock;
    }

    private static DiscountContext ContextWorth27Euros50()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.Of(2)); // 20.00
        order.AddItem(Dish.Create("Soda", Money.Of(2.50m, "EUR")), Quantity.Of(3));  //  7.50
        return DiscountContext.For(order);
    }
}
