using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Tests.Menu;

public sealed class ComboTests
{
    private readonly Dish _burger = Dish.Create("Burger", Money.Of(10m, "EUR"));
    private readonly Dish _fries = Dish.Create("Fries", Money.Of(3.50m, "EUR"));
    private readonly Dish _drink = Dish.Create("Drink", Money.Of(2.50m, "EUR"));

    [Fact]
    public void Create_WithValidComponents_CreatesCombo()
    {
        var combo = BurgerMenu();

        combo.Name.Should().Be("Burger menu");
        combo.DiscountPercentage.Should().Be(15m);
        combo.Components.Should().HaveCount(3);
        combo.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void GetPrice_SumsComponentsAndAppliesDiscount()
    {
        // 10.00 + 3.50 + 2.50 = 16.00, minus 15% (2.40) = 13.60
        var combo = BurgerMenu();

        combo.GetPrice().Should().Be(Money.Of(13.60m, "EUR"));
    }

    [Fact]
    public void GetPrice_MultipliesComponentPriceByQuantity()
    {
        // 2 × 10.00 + 1 × 2.50 = 22.50, no discount
        var combo = Combo.Create("Double burger", 0m, [(_burger, Quantity.Of(2)), (_drink, Quantity.One)]);

        combo.GetPrice().Should().Be(Money.Of(22.50m, "EUR"));
    }

    [Fact]
    public void GetPrice_WithNestedCombo_CalculatesRecursively()
    {
        // Inner "Burger menu" = 13.60. Outer = 13.60 + 3.50 = 17.10, minus 10% (1.71) = 15.39
        var outer = Combo.Create("Burger menu + extra fries", 10m, [(BurgerMenu(), Quantity.One), (_fries, Quantity.One)]);

        outer.GetPrice().Should().Be(Money.Of(15.39m, "EUR"));
    }

    [Fact]
    public void GetPrice_CalledThroughBaseType_WorksLikeAnyMenuItem()
    {
        // Composite + LSP: code that knows only MenuItem works for dishes and combos alike.
        IReadOnlyList<MenuItem> items = [_burger, BurgerMenu()];

        var prices = items.Select(item => item.GetPrice());

        prices.Should().Equal(Money.Of(10m, "EUR"), Money.Of(13.60m, "EUR"));
    }

    [Fact]
    public void GetPrice_AfterComponentPriceChanges_ReflectsNewPrice()
    {
        var combo = BurgerMenu();

        _burger.ChangePrice(Money.Of(12m, "EUR"));

        // 12.00 + 3.50 + 2.50 = 18.00, minus 15% (2.70) = 15.30
        combo.GetPrice().Should().Be(Money.Of(15.30m, "EUR"));
    }

    [Fact]
    public void Create_WithSameItemTwice_MergesIntoOneComponent()
    {
        var combo = Combo.Create("Two burgers", 0m, [(_burger, Quantity.One), (_burger, Quantity.One)]);

        combo.Components.Should().ContainSingle()
            .Which.Quantity.Should().Be(Quantity.Of(2));
    }

    [Fact]
    public void Create_WithFewerThanMinimumUnits_ThrowsDomainException()
    {
        var act = () => Combo.Create("Lonely burger", 0m, [(_burger, Quantity.One)]);

        act.Should().Throw<DomainException>().WithMessage("*at least 2 units*");
    }

    [Fact]
    public void Create_WithNoComponents_ThrowsDomainException()
    {
        var act = () => Combo.Create("Empty", 0m, []);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    [InlineData(150)]
    public void Create_WithInvalidDiscount_ThrowsDomainException(decimal discount)
    {
        var act = () => Combo.Create("Combo", discount, [(_burger, Quantity.One), (_drink, Quantity.One)]);

        act.Should().Throw<DomainException>().WithMessage("*discount*");
    }

    [Fact]
    public void Create_WithMixedCurrencies_ThrowsDomainException()
    {
        var dollarDrink = Dish.Create("Soda", Money.Of(2m, "USD"));

        var act = () => Combo.Create("Combo", 0m, [(_burger, Quantity.One), (dollarDrink, Quantity.One)]);

        act.Should().Throw<DomainException>().WithMessage("*same currency*");
    }

    [Fact]
    public void AddComponent_WithItself_ThrowsDomainException()
    {
        var combo = BurgerMenu();

        var act = () => combo.AddComponent(combo, Quantity.One);

        act.Should().Throw<DomainException>().WithMessage("*cannot contain itself*");
    }

    [Fact]
    public void AddComponent_CreatingIndirectCycle_ThrowsDomainException()
    {
        // inner is inside outer; adding outer into inner would create inner -> outer -> inner -> ...
        var inner = BurgerMenu();
        var outer = Combo.Create("Big menu", 0m, [(inner, Quantity.One), (_fries, Quantity.One)]);

        var act = () => inner.AddComponent(outer, Quantity.One);

        act.Should().Throw<DomainException>().WithMessage("*cannot contain itself*");
    }

    [Fact]
    public void Contains_FindsItemsAtAnyDepth()
    {
        var inner = BurgerMenu();
        var outer = Combo.Create("Big menu", 0m, [(inner, Quantity.One), (_fries, Quantity.One)]);

        outer.Contains(inner).Should().BeTrue();
        outer.Contains(_burger).Should().BeTrue("the burger is inside the nested combo");
        _burger.Contains(outer).Should().BeFalse("a dish (leaf) contains nothing");
    }

    [Fact]
    public void RemoveComponent_LeavingEnoughUnits_RemovesIt()
    {
        var combo = BurgerMenu();

        combo.RemoveComponent(_fries.Id);

        combo.Components.Select(c => c.Item).Should().BeEquivalentTo(new MenuItem[] { _burger, _drink });
    }

    [Fact]
    public void RemoveComponent_LeavingTooFewUnits_ThrowsDomainException()
    {
        var combo = Combo.Create("Burger and drink", 0m, [(_burger, Quantity.One), (_drink, Quantity.One)]);

        var act = () => combo.RemoveComponent(_drink.Id);

        act.Should().Throw<DomainException>();
        combo.Components.Should().HaveCount(2, "a failed operation must leave the combo unchanged");
    }

    [Fact]
    public void RemoveComponent_NotInCombo_ThrowsDomainException()
    {
        var combo = BurgerMenu();

        var act = () => combo.RemoveComponent(Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("*does not contain*");
    }

    [Fact]
    public void CanBeOrdered_WhenAllComponentsAvailable_ReturnsTrue()
    {
        BurgerMenu().CanBeOrdered.Should().BeTrue();
    }

    [Fact]
    public void CanBeOrdered_WhenAComponentIsUnavailable_ReturnsFalse()
    {
        var combo = BurgerMenu();

        _fries.MarkUnavailable();

        combo.IsAvailable.Should().BeTrue("the combo itself was not switched off");
        combo.CanBeOrdered.Should().BeFalse("it cannot be served without fries");
    }

    [Fact]
    public void CanBeOrdered_WhenANestedComponentIsUnavailable_ReturnsFalse()
    {
        var outer = Combo.Create("Big menu", 0m, [(BurgerMenu(), Quantity.One), (_fries, Quantity.One)]);

        _drink.MarkUnavailable(); // lives inside the nested Burger menu

        outer.CanBeOrdered.Should().BeFalse();
    }

    [Fact]
    public void CanBeOrdered_WhenComboIsMarkedUnavailable_ReturnsFalse()
    {
        var combo = BurgerMenu();

        combo.MarkUnavailable();

        combo.CanBeOrdered.Should().BeFalse();
    }

    private Combo BurgerMenu() =>
        Combo.Create("Burger menu", 15m, [(_burger, Quantity.One), (_fries, Quantity.One), (_drink, Quantity.One)]);
}
