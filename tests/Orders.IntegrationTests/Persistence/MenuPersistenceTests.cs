using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.Orders.Infrastructure.Persistence.Repositories;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.IntegrationTests.Persistence;

/// <summary>
/// The menu hierarchy (TPH) and the Composite tree must survive a trip to SQL Server, including combos
/// nested inside combos, which Include alone cannot load (see MenuItemRepository).
/// </summary>
public sealed class MenuPersistenceTests(SqlServerFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetById_Dish_LoadsTheRightSubclassWithItsPrice()
    {
        var dish = Dish.Create("Grilled salmon", Money.Of(14.90m, "EUR"));
        dish.EnableTwoForOne();
        await SaveAsync(dish);

        var loaded = await LoadAsync(dish.Id);

        loaded.Should().BeOfType<Dish>("the 'Kind' discriminator tells EF which class to create");
        loaded.GetPrice().Should().Be(Money.Of(14.90m, "EUR"));
        loaded.IsTwoForOneEligible.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_NestedCombo_LoadsTheWholeCompositeTree()
    {
        var burger = Dish.Create("Test burger", Money.Of(10m, "EUR"));
        var fries = Dish.Create("Test fries", Money.Of(3.50m, "EUR"));
        var soda = Dish.Create("Test soda", Money.Of(2.50m, "EUR"));
        var salad = Dish.Create("Test salad", Money.Of(7m, "EUR"));
        var menu = Combo.Create("Test menu", 15m, [(burger, Quantity.One), (fries, Quantity.One), (soda, Quantity.One)]);
        var family = Combo.Create("Test family", 10m, [(menu, Quantity.Of(2)), (salad, Quantity.One)]);
        await SaveAsync(burger, fries, soda, salad, menu, family);
        var expectedPrice = family.GetPrice(); // calculated in memory before saving

        var loaded = await LoadAsync(family.Id);

        var loadedFamily = loaded.Should().BeOfType<Combo>().Subject;
        var nestedMenu = loadedFamily.Components.Single(c => c.Item.Name == "Test menu").Item;
        nestedMenu.Should().BeOfType<Combo>()
            .Which.Components.Should().HaveCount(3, "the nested combo's own components must be loaded too");

        // The recursive price can only match if every level of the tree came back from the database.
        loadedFamily.GetPrice().Should().Be(expectedPrice);
        loadedFamily.Contains(burger).Should().BeTrue("the burger sits two levels down");
    }

    [Fact]
    public async Task GetById_ComboWithUnavailableComponent_CannotBeOrdered()
    {
        var coffee = Dish.Create("Test coffee", Money.Of(1.80m, "EUR"));
        var cake = Dish.Create("Test cake", Money.Of(4m, "EUR"));
        var snack = Combo.Create("Test snack", 0m, [(coffee, Quantity.One), (cake, Quantity.One)]);
        cake.MarkUnavailable();
        await SaveAsync(coffee, cake, snack);

        var loaded = await LoadAsync(snack.Id);

        loaded.CanBeOrdered.Should().BeFalse();
    }

    [Fact]
    public async Task GetAll_ReturnsDishesAndCombos()
    {
        var dish = Dish.Create("Test soup", Money.Of(5m, "EUR"));
        var combo = Combo.Create("Test soup menu", 5m, [(dish, Quantity.Of(2))]);
        await SaveAsync(dish, combo);

        await using var context = database.CreateContext();
        var all = await new MenuItemRepository(context).GetAllAsync(Ct);

        all.Select(item => item.Id).Should().Contain([dish.Id, combo.Id]);
    }

    private async Task SaveAsync(params MenuItem[] items)
    {
        await using var context = database.CreateContext();
        context.MenuItems.AddRange(items);
        await context.SaveChangesAsync(Ct);
    }

    private async Task<MenuItem> LoadAsync(Guid id)
    {
        await using var context = database.CreateContext();
        var loaded = await new MenuItemRepository(context).GetByIdAsync(id, Ct);
        loaded.Should().NotBeNull();
        return loaded!;
    }
}
