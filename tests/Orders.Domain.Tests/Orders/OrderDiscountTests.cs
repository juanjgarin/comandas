using Comandas.Orders.Domain.Discounts;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace Comandas.Orders.Domain.Tests.Orders;

/// <summary>The order stores the discount as a snapshot and keeps Total = Subtotal - Discount consistent.</summary>
public sealed class OrderDiscountTests
{
    private readonly Dish _burger = Dish.Create("Burger", Money.Of(10m, "EUR"));

    [Fact]
    public void Total_WithoutDiscount_EqualsSubtotal()
    {
        var order = OrderWithBurgers(2);

        order.Discount.Should().BeNull();
        order.Total.Should().Be(Money.Of(20m, "EUR"));
    }

    [Fact]
    public void ApplyDiscount_Percentage_StoresSnapshotAndReducesTotal()
    {
        var order = OrderWithBurgers(2);

        order.ApplyDiscount(new PercentageDiscount(10m));

        order.Discount.Should().Be(AppliedDiscount.Of("10% off", Money.Of(2m, "EUR")));
        order.Subtotal.Should().Be(Money.Of(20m, "EUR"), "the subtotal is always before discounts");
        order.Total.Should().Be(Money.Of(18m, "EUR"));
    }

    [Fact]
    public void ApplyDiscount_FullDiscount_TotalIsZero()
    {
        var order = OrderWithBurgers(1);

        order.ApplyDiscount(new PercentageDiscount(100m));

        order.Total.Should().Be(Money.Zero("EUR"));
    }

    [Fact]
    public void ApplyDiscount_NoDiscountNullObject_LeavesOrderWithoutDiscount()
    {
        var order = OrderWithBurgers(2);
        order.ApplyDiscount(new PercentageDiscount(10m));

        order.ApplyDiscount(NoDiscount.Instance);

        order.Discount.Should().BeNull("a discount that saves nothing is not stored");
        order.Total.Should().Be(order.Subtotal);
    }

    [Fact]
    public void ApplyDiscount_Twice_ReplacesThePreviousDiscount()
    {
        var order = OrderWithBurgers(2);
        order.ApplyDiscount(new PercentageDiscount(10m));

        order.ApplyDiscount(new PercentageDiscount(25m));

        order.Discount!.Description.Should().Be("25% off"); // Applied just above.
        order.Total.Should().Be(Money.Of(15m, "EUR"), "discounts are not combinable (D6)");
    }

    [Fact]
    public void ApplyDiscount_HappyHour_IsASnapshotThatSurvivesTheEndOfTheWindow()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 11, 26, 19, 0, 0, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        var happyHour = new HappyHourDiscount(clock, new TimeOnly(18, 0), new TimeOnly(20, 0), 20m);
        var order = OrderWithBurgers(2);

        order.ApplyDiscount(happyHour);
        clock.Advance(TimeSpan.FromHours(2)); // 21:00, happy hour is over

        order.Total.Should().Be(Money.Of(16m, "EUR"), "the bill keeps the discount granted when it was applied");
    }

    [Fact]
    public void ApplyDiscount_WhenConfirmed_ThrowsDomainException()
    {
        // Same rule as the lines: once confirmed, what the customer ordered and pays is fixed.
        var order = OrderWithBurgers(2);
        order.Confirm();

        var act = () => order.ApplyDiscount(new PercentageDiscount(10m));

        act.Should().Throw<DomainException>().WithMessage("*Confirmed*");
        order.Discount.Should().BeNull();
    }

    [Fact]
    public void Confirm_WithDiscount_KeepsTheDiscount()
    {
        var order = OrderWithBurgers(2);
        order.ApplyDiscount(new PercentageDiscount(10m));

        order.Confirm();

        order.Total.Should().Be(Money.Of(18m, "EUR"));
    }

    [Fact]
    public void ApplyDiscount_StrategyLargerThanSubtotal_ThrowsDomainException()
    {
        var order = OrderWithBurgers(1);

        var act = () => order.ApplyDiscount(new FixedAmountStrategy(Money.Of(15m, "EUR")));

        act.Should().Throw<DomainException>().WithMessage("*larger than the subtotal*");
        order.Discount.Should().BeNull();
    }

    [Fact]
    public void ApplyDiscount_StrategyInAnotherCurrency_ThrowsDomainException()
    {
        var order = OrderWithBurgers(1);

        var act = () => order.ApplyDiscount(new FixedAmountStrategy(Money.Of(1m, "USD")));

        act.Should().Throw<DomainException>().WithMessage("*USD*EUR*");
    }

    [Fact]
    public void AddItem_AfterDiscount_ClearsTheDiscount()
    {
        var order = OrderWithBurgers(2);
        order.ApplyDiscount(new PercentageDiscount(10m));

        order.AddItem(_burger, Quantity.One);

        order.Discount.Should().BeNull("the amount was calculated over lines that no longer exist as they were");
        order.Total.Should().Be(Money.Of(30m, "EUR"));
    }

    [Fact]
    public void ChangeLineQuantity_AfterDiscount_ClearsTheDiscount()
    {
        var order = Order.Create(1, "EUR");
        var line = order.AddItem(_burger, Quantity.Of(2));
        order.ApplyDiscount(new PercentageDiscount(10m));

        order.ChangeLineQuantity(line.Id, Quantity.Of(3));

        order.Discount.Should().BeNull();
    }

    [Fact]
    public void RemoveLine_AfterDiscount_ClearsTheDiscount()
    {
        // The case that motivates the rule: the 2-for-1 pair is gone, so the old amount would be a wrong bill.
        var soda = Dish.Create("Soda", Money.Of(2.50m, "EUR"));
        soda.EnableTwoForOne();
        var order = OrderWithBurgers(1);
        var sodas = order.AddItem(soda, Quantity.Of(2));
        order.ApplyDiscount(new TwoForOneDiscount());

        order.RemoveLine(sodas.Id);

        order.Discount.Should().BeNull();
        order.Total.Should().Be(Money.Of(10m, "EUR"));
    }

    [Fact]
    public void ChangeLineQuantity_RejectedEdit_KeepsTheDiscount()
    {
        var order = OrderWithBurgers(2);
        order.ApplyDiscount(new PercentageDiscount(10m));

        var act = () => order.ChangeLineQuantity(Guid.NewGuid(), Quantity.One);

        act.Should().Throw<DomainException>();
        order.Discount.Should().NotBeNull("nothing changed, so the discount is still right");
    }

    private Order OrderWithBurgers(int burgers)
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(_burger, Quantity.Of(burgers));
        return order;
    }

    /// <summary>A deliberately misbehaving strategy: proves the order does not trust strategies blindly.</summary>
    private sealed class FixedAmountStrategy(Money amount) : IDiscountStrategy
    {
        public string Description => "Fixed amount";

        public Money CalculateDiscount(DiscountContext context) => amount;
    }
}
