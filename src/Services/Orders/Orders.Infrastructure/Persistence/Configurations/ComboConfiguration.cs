using Comandas.Orders.Domain.Menu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandas.Orders.Infrastructure.Persistence.Configurations;

/// <summary>Combo-only columns of the shared MenuItems table, plus its components (the Composite's children).</summary>
internal sealed class ComboConfiguration : IEntityTypeConfiguration<Combo>
{
    public void Configure(EntityTypeBuilder<Combo> builder)
    {
        builder.Property(combo => combo.DiscountPercentage)
            .HasPrecision(5, 2);

        builder.HasMany(combo => combo.Components)
            .WithOne()
            .HasForeignKey("ComboId")
            .IsRequired() // a component always belongs to a combo
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(combo => combo.Components).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
