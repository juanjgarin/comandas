using Comandas.Orders.Domain.Discounts;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Tests.Discounts;

public sealed class PercentageDiscountTests
{
    [Theory]
    [InlineData(10, 2.75)]   // 10% of 27.50
    [InlineData(15, 4.13)]   // 4.125 rounds half away from zero
    [InlineData(100, 27.50)] // on the house
    public void CalculateDiscount_ReturnsPercentageOfSubtotal(decimal percentage, decimal expected)
    {
        var discount = new PercentageDiscount(percentage);

        var amount = discount.CalculateDiscount(ContextFor(OrderWorth27Euros50()));

        amount.Should().Be(Money.Of(expected, "EUR"));
    }

    [Fact]
    public void CalculateDiscount_EmptyOrder_ReturnsZero()
    {
        var amount = new PercentageDiscount(10m).CalculateDiscount(ContextFor(Order.Create(1, "EUR")));

        amount.Should().Be(Money.Zero("EUR"));
    }

    [Fact]
    public void CalculateDiscount_DoesNotChangeTheOrder()
    {
        var order = OrderWorth27Euros50();

        new PercentageDiscount(50m).CalculateDiscount(ContextFor(order));

        order.Subtotal.Should().Be(Money.Of(27.50m, "EUR"), "a strategy only calculates; it never modifies");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.01)]
    public void Constructor_WithInvalidPercentage_ThrowsDomainException(decimal percentage)
    {
        var act = () => new PercentageDiscount(percentage);

        act.Should().Throw<DomainException>().WithMessage("*greater than 0%*at most 100%*");
    }

    [Theory]
    [InlineData(10, "10% off")]
    [InlineData(12.5, "12.5% off")]
    public void Description_ShowsThePercentage(decimal percentage, string expected)
    {
        new PercentageDiscount(percentage).Description.Should().Be(expected);
    }

    [Fact]
    public void UsedThroughTheInterface_WorksLikeAnyStrategy()
    {
        var amount = ApplyAnyStrategy(new PercentageDiscount(20m), ContextFor(OrderWorth27Euros50()));

        amount.Should().Be(Money.Of(5.50m, "EUR"));
    }

    // Stands for any client code: it depends on the abstraction only, so any IDiscountStrategy can be plugged in.
    private static Money ApplyAnyStrategy(IDiscountStrategy strategy, DiscountContext context) =>
        strategy.CalculateDiscount(context);

    private static Order OrderWorth27Euros50()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.Of(2)); // 20.00
        order.AddItem(Dish.Create("Soda", Money.Of(2.50m, "EUR")), Quantity.Of(3));  //  7.50
        return order;
    }

    private static DiscountContext ContextFor(Order order) => DiscountContext.For(order);
}
