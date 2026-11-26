using Comandas.Orders.Domain.Menu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandas.Orders.Infrastructure.Persistence.Configurations;

/// <summary>Dish-only columns of the shared MenuItems table (NULL in combo rows: the price of TPH).</summary>
internal sealed class DishConfiguration : IEntityTypeConfiguration<Dish>
{
    public void Configure(EntityTypeBuilder<Dish> builder)
    {
        builder.OwnsOne(dish => dish.BasePrice, money => money.MapMoney("BasePrice"));
        builder.Navigation(dish => dish.BasePrice).IsRequired();
    }
}
