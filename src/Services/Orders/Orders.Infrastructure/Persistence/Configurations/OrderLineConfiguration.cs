using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandas.Orders.Infrastructure.Persistence.Configurations;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");

        builder.HasKey(line => line.Id);
        builder.Property(line => line.Id).ValueGeneratedNever();

        // Reference to the menu item by id only, with no foreign key: the line keeps its own snapshot,
        // so a menu item may change (or, in phase 4, live in another service's database).
        builder.Property(line => line.MenuItemId);

        builder.Property(line => line.ItemName)
            .HasMaxLength(MenuItem.NameMaxLength);

        builder.OwnsOne(line => line.UnitPrice, money => money.MapMoney("UnitPrice"));
        builder.Navigation(line => line.UnitPrice).IsRequired();

        builder.Property(line => line.Quantity)
            .HasConversion(ValueObjectMappings.QuantityConverter);

        builder.Ignore(line => line.LineTotal);
    }
}
