using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Comandas.Orders.Infrastructure.Persistence.Seeding;

/// <summary>
/// Sample menu for development. Built with the DOMAIN factories (Dish.Create, Combo.Create), so seed data
/// goes through exactly the same rules as real data, unlike EF's HasData, which writes raw rows.
/// Idempotent: does nothing if the menu already has items.
/// </summary>
internal static class MenuSeeder
{
    // Must match the "Restaurant:Currency" setting (default EUR), or orders could not include these items.
    private const string Currency = "EUR";

    public static void Seed(OrdersDbContext context)
    {
        if (context.MenuItems.Any())
        {
            return;
        }

        context.MenuItems.AddRange(BuildMenu());
        context.SaveChanges();
    }

    public static async Task SeedAsync(OrdersDbContext context, CancellationToken cancellationToken)
    {
        if (await context.MenuItems.AnyAsync(cancellationToken))
        {
            return;
        }

        context.MenuItems.AddRange(BuildMenu());
        await context.SaveChangesAsync(cancellationToken);
    }

    private static List<MenuItem> BuildMenu()
    {
        var classicBurger = Dish.Create("Classic burger", Euros(9.50m));
        var cheeseburger = Dish.Create("Cheeseburger", Euros(10.50m));
        var veggieBurger = Dish.Create("Veggie burger", Euros(9.90m));
        var fries = Dish.Create("French fries", Euros(3.50m));
        var caesarSalad = Dish.Create("Caesar salad", Euros(7.80m));
        var soda = Dish.Create("Soda", Euros(2.50m));
        var draftBeer = Dish.Create("Draft beer", Euros(3.20m));
        var coffee = Dish.Create("Coffee", Euros(1.80m));
        var brownie = Dish.Create("Brownie", Euros(4.50m));

        // Flagged for the "2-for-1" discount (phase 2).
        soda.EnableTwoForOne();
        draftBeer.EnableTwoForOne();

        var burgerMenu = Combo.Create(
            "Burger menu", 15m, [(classicBurger, Quantity.One), (fries, Quantity.One), (soda, Quantity.One)]);

        var cheeseburgerMenu = Combo.Create(
            "Cheeseburger menu", 15m, [(cheeseburger, Quantity.One), (fries, Quantity.One), (soda, Quantity.One)]);

        // A combo inside a combo: exercises the Composite pattern at two levels.
        var familyPack = Combo.Create(
            "Family pack", 10m, [(burgerMenu, Quantity.Of(2)), (caesarSalad, Quantity.One), (brownie, Quantity.One)]);

        return
        [
            classicBurger, cheeseburger, veggieBurger, fries, caesarSalad, soda, draftBeer, coffee, brownie,
            burgerMenu, cheeseburgerMenu, familyPack,
        ];
    }

    private static Money Euros(decimal amount) => Money.Of(amount, Currency);
}
