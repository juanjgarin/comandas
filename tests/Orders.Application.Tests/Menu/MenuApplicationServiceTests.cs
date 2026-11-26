using Comandas.Orders.Application.Menu;
using Comandas.Orders.Application.Tests.Fakes;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Application.Tests.Menu;

public sealed class MenuApplicationServiceTests
{
    [Fact]
    public async Task GetMenuAsync_ReturnsDishesAndCombosSortedByNameWithCalculatedPrices()
    {
        var burger = Dish.Create("Burger", Money.Of(10m, "EUR"));
        var drink = Dish.Create("Drink", Money.Of(2.50m, "EUR"));
        var combo = Combo.Create("Burger menu", 10m, [(burger, Quantity.One), (drink, Quantity.One)]);
        drink.EnableTwoForOne();
        var service = new MenuApplicationService(new InMemoryMenuItemRepository(drink, combo, burger));

        var menu = await service.GetMenuAsync(TestContext.Current.CancellationToken);

        menu.Should().Equal(
            new MenuItemDto(burger.Id, "Burger", "Dish", 10m, "EUR", CanBeOrdered: true, IsTwoForOneEligible: false),
            new MenuItemDto(combo.Id, "Burger menu", "Combo", 11.25m, "EUR", CanBeOrdered: true, IsTwoForOneEligible: false),
            new MenuItemDto(drink.Id, "Drink", "Dish", 2.50m, "EUR", CanBeOrdered: true, IsTwoForOneEligible: true));
    }

    [Fact]
    public async Task GetMenuAsync_ComboWithUnavailableComponent_IsNotOrderable()
    {
        var burger = Dish.Create("Burger", Money.Of(10m, "EUR"));
        var drink = Dish.Create("Drink", Money.Of(2.50m, "EUR"));
        var combo = Combo.Create("Burger menu", 10m, [(burger, Quantity.One), (drink, Quantity.One)]);
        drink.MarkUnavailable();
        var service = new MenuApplicationService(new InMemoryMenuItemRepository(burger, drink, combo));

        var menu = await service.GetMenuAsync(TestContext.Current.CancellationToken);

        menu.Single(item => item.Id == combo.Id).CanBeOrdered.Should().BeFalse();
    }
}
