using Comandas.Orders.Domain.Menu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandas.Orders.Infrastructure.Persistence.Configurations;

/// <summary>
/// "2 × Burger" inside a combo. Two foreign keys to MenuItems: the combo that owns it (cascade delete)
/// and the item it contains (restrict: a dish used by a combo cannot be deleted).
/// </summary>
internal sealed class ComboComponentConfiguration : IEntityTypeConfiguration<ComboComponent>
{
    public void Configure(EntityTypeBuilder<ComboComponent> builder)
    {
        builder.ToTable("ComboComponents");

        builder.HasKey(component => component.Id);
        builder.Property(component => component.Id).ValueGeneratedNever();

        builder.HasOne(component => component.Item)
            .WithMany()
            .HasForeignKey("MenuItemId")
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(component => component.Quantity)
            .HasConversion(ValueObjectMappings.QuantityConverter);
    }
}
