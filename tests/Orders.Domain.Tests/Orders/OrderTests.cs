using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Tests.Orders;

public sealed class OrderTests
{
    private readonly Dish _burger = Dish.Create("Burger", Money.Of(10m, "EUR"));
    private readonly Dish _drink = Dish.Create("Drink", Money.Of(2.50m, "EUR"));

    [Fact]
    public void Create_WithValidTableAndCurrency_CreatesEmptyOrder()
    {
        var order = Order.Create(12, "eur");

        order.Id.Should().NotBeEmpty();
        order.TableNumber.Should().Be(12);
        order.Currency.Should().Be("EUR");
        order.Lines.Should().BeEmpty();
        order.Subtotal.Should().Be(Money.Zero("EUR"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(1000)]
    public void Create_WithInvalidTableNumber_ThrowsDomainException(int tableNumber)
    {
        var act = () => Order.Create(tableNumber, "EUR");

        act.Should().Throw<DomainException>().WithMessage("*Table number*");
    }

    [Fact]
    public void Create_WithInvalidCurrency_ThrowsDomainException()
    {
        var act = () => Order.Create(1, "EURO");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddItem_NewItem_AddsLineWithSnapshotOfNameAndPrice()
    {
        var order = Order.Create(1, "EUR");

        var line = order.AddItem(_burger, Quantity.Of(2));

        line.MenuItemId.Should().Be(_burger.Id);
        line.ItemName.Should().Be("Burger");
        line.UnitPrice.Should().Be(Money.Of(10m, "EUR"));
        line.Quantity.Should().Be(Quantity.Of(2));
        line.LineTotal.Should().Be(Money.Of(20m, "EUR"));
        order.Lines.Should().ContainSingle();
    }

    [Fact]
    public void AddItem_Combo_UsesTheComboCalculatedPrice()
    {
        var combo = Combo.Create("Burger menu", 10m, [(_burger, Quantity.One), (_drink, Quantity.One)]);
        var order = Order.Create(1, "EUR");

        var line = order.AddItem(combo, Quantity.One);

        // (10.00 + 2.50) - 10% = 11.25: the order does not care that it is a combo (polymorphism)
        line.UnitPrice.Should().Be(Money.Of(11.25m, "EUR"));
    }

    [Fact]
    public void AddItem_SameItemAndPriceTwice_IncreasesQuantityOfExistingLine()
    {
        var order = Order.Create(1, "EUR");

        order.AddItem(_burger, Quantity.One);
        order.AddItem(_burger, Quantity.Of(2));

        order.Lines.Should().ContainSingle()
            .Which.Quantity.Should().Be(Quantity.Of(3));
    }

    [Fact]
    public void AddItem_SameItemAfterPriceChange_AddsSeparateLineAtNewPrice()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(_burger, Quantity.One);

        _burger.ChangePrice(Money.Of(12m, "EUR"));
        order.AddItem(_burger, Quantity.One);

        order.Lines.Select(line => line.UnitPrice)
            .Should().Equal(Money.Of(10m, "EUR"), Money.Of(12m, "EUR"));
    }

    [Fact]
    public void AddItem_SameItemAfterTwoForOneFlagChanges_AddsSeparateLine()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(_drink, Quantity.One);

        _drink.EnableTwoForOne();
        order.AddItem(_drink, Quantity.One);

        order.Lines.Select(line => line.IsTwoForOneEligible).Should().Equal(false, true);
    }

    [Fact]
    public void AddItem_SnapshotsTheTwoForOneFlag()
    {
        _drink.EnableTwoForOne();
        var order = Order.Create(1, "EUR");

        var line = order.AddItem(_drink, Quantity.One);
        _drink.DisableTwoForOne();

        line.IsTwoForOneEligible.Should().BeTrue();
    }

    [Fact]
    public void AddItem_WhenMenuPriceChangesLater_KeepsTheSnapshotPrice()
    {
        var order = Order.Create(1, "EUR");
        var line = order.AddItem(_burger, Quantity.One);

        _burger.ChangePrice(Money.Of(15m, "EUR"));
        _burger.Rename("Deluxe burger");

        line.UnitPrice.Should().Be(Money.Of(10m, "EUR"), "what was sold does not change with the menu");
        line.ItemName.Should().Be("Burger");
    }

    [Fact]
    public void AddItem_ItemThatCannotBeOrdered_ThrowsDomainException()
    {
        var order = Order.Create(1, "EUR");
        _burger.MarkUnavailable();

        var act = () => order.AddItem(_burger, Quantity.One);

        act.Should().Throw<DomainException>().WithMessage("*cannot be ordered*");
        order.Lines.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_ItemInDifferentCurrency_ThrowsDomainException()
    {
        var order = Order.Create(1, "EUR");
        var dollarSoda = Dish.Create("Soda", Money.Of(2m, "USD"));

        var act = () => order.AddItem(dollarSoda, Quantity.One);

        act.Should().Throw<DomainException>().WithMessage("*USD*EUR*");
    }

    [Fact]
    public void AddItem_MergeExceedingMaxQuantity_ThrowsDomainExceptionAndKeepsLine()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(_burger, Quantity.Of(90));

        var act = () => order.AddItem(_burger, Quantity.Of(10));

        act.Should().Throw<DomainException>();
        order.Lines.Single().Quantity.Should().Be(Quantity.Of(90));
    }

    [Fact]
    public void Subtotal_SumsAllLineTotals()
    {
        var order = Order.Create(1, "EUR");

        order.AddItem(_burger, Quantity.Of(2));  // 20.00
        order.AddItem(_drink, Quantity.Of(3));   //  7.50

        order.Subtotal.Should().Be(Money.Of(27.50m, "EUR"));
    }

    [Fact]
    public void ChangeLineQuantity_ExistingLine_UpdatesQuantityAndSubtotal()
    {
        var order = Order.Create(1, "EUR");
        var line = order.AddItem(_burger, Quantity.One);

        order.ChangeLineQuantity(line.Id, Quantity.Of(4));

        line.Quantity.Should().Be(Quantity.Of(4));
        order.Subtotal.Should().Be(Money.Of(40m, "EUR"));
    }

    [Fact]
    public void ChangeLineQuantity_UnknownLine_ThrowsDomainException()
    {
        var order = Order.Create(1, "EUR");

        var act = () => order.ChangeLineQuantity(Guid.NewGuid(), Quantity.One);

        act.Should().Throw<DomainException>().WithMessage("*has no line*");
    }

    [Fact]
    public void RemoveLine_ExistingLine_RemovesItFromOrder()
    {
        var order = Order.Create(1, "EUR");
        var burgerLine = order.AddItem(_burger, Quantity.One);
        order.AddItem(_drink, Quantity.One);

        order.RemoveLine(burgerLine.Id);

        order.Lines.Should().ContainSingle().Which.ItemName.Should().Be("Drink");
        order.Subtotal.Should().Be(Money.Of(2.50m, "EUR"));
    }

    [Fact]
    public void RemoveLine_UnknownLine_ThrowsDomainException()
    {
        var order = Order.Create(1, "EUR");

        var act = () => order.RemoveLine(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Lines_CannotBeModifiedFromOutside()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(_burger, Quantity.One);

        // Encapsulation: even casting the exposed collection does not allow changing it;
        // the only way in is through Order's methods.
        var hackedCollection = (ICollection<OrderLine>)order.Lines;

        var act = () => hackedCollection.Clear();

        act.Should().Throw<NotSupportedException>();
        order.Lines.Should().ContainSingle();
    }
}
