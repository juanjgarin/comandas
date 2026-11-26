using Comandas.Orders.Domain.Discounts;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Tests.Discounts;

public sealed class TwoForOneDiscountTests
{
    private readonly TwoForOneDiscount _twoForOne = new();

    [Theory]
    [InlineData(1, 0)]     // a single unit: nothing free
    [InlineData(2, 2.50)]  // 1 free
    [InlineData(3, 2.50)]  // still 1 free: the third is paid
    [InlineData(4, 5.00)]  // 2 free
    [InlineData(5, 5.00)]
    public void CalculateDiscount_EligibleLine_GivesOneFreeUnitPerPair(int quantity, decimal expected)
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(EligibleSoda(), Quantity.Of(quantity));

        var amount = _twoForOne.CalculateDiscount(DiscountContext.For(order));

        amount.Should().Be(Money.Of(expected, "EUR"));
    }

    [Fact]
    public void CalculateDiscount_MixedOrder_CountsEachEligibleLineAtItsOwnPrice()
    {
        var beer = Dish.Create("Beer", Money.Of(3.20m, "EUR"));
        beer.EnableTwoForOne();
        var order = Order.Create(1, "EUR");
        order.AddItem(EligibleSoda(), Quantity.Of(3));                             // 1 free x 2.50
        order.AddItem(beer, Quantity.Of(2));                                       // 1 free x 3.20
        order.AddItem(Dish.Create("Coffee", Money.Of(1.80m, "EUR")), Quantity.Of(4)); // not eligible

        var amount = _twoForOne.CalculateDiscount(DiscountContext.For(order));

        amount.Should().Be(Money.Of(5.70m, "EUR"));
    }

    [Fact]
    public void CalculateDiscount_NoEligibleLines_ReturnsZero()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(Dish.Create("Coffee", Money.Of(1.80m, "EUR")), Quantity.Of(6));

        _twoForOne.CalculateDiscount(DiscountContext.For(order)).Should().Be(Money.Zero("EUR"));
    }

    [Fact]
    public void CalculateDiscount_EligibleCombo_UsesTheComboPrice()
    {
        var combo = Combo.Create(
            "Snack", 0m, [(Dish.Create("Coffee", Money.Of(2m, "EUR")), Quantity.One), (Dish.Create("Cake", Money.Of(4m, "EUR")), Quantity.One)]);
        combo.EnableTwoForOne();
        var order = Order.Create(1, "EUR");
        order.AddItem(combo, Quantity.Of(2));

        _twoForOne.CalculateDiscount(DiscountContext.For(order)).Should().Be(Money.Of(6m, "EUR"));
    }

    [Fact]
    public void CalculateDiscount_FlagSwitchedOffAfterOrdering_StillUsesTheSnapshot()
    {
        var soda = EligibleSoda();
        var order = Order.Create(1, "EUR");
        order.AddItem(soda, Quantity.Of(2));

        soda.DisableTwoForOne(); // the restaurant ends the promotion

        _twoForOne.CalculateDiscount(DiscountContext.For(order))
            .Should().Be(Money.Of(2.50m, "EUR"), "orders already taken keep the conditions they were taken with");
    }

    [Fact]
    public void Description_DescribesThePromotion()
    {
        _twoForOne.Description.Should().Be("2-for-1 on selected items");
    }

    private static Dish EligibleSoda()
    {
        var soda = Dish.Create("Soda", Money.Of(2.50m, "EUR"));
        soda.EnableTwoForOne();
        return soda;
    }
}
