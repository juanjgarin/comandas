using Comandas.Orders.Domain.Menu;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandas.Orders.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base of the menu hierarchy, mapped as TPH (decision D4): ONE table "MenuItems" for dishes and combos,
/// with a "Kind" discriminator column telling EF which class to create for each row.
/// </summary>
internal sealed class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public const string DiscriminatorColumn = "Kind";

    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");

        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();

        builder.HasDiscriminator<string>(DiscriminatorColumn)
            .HasValue<Dish>(nameof(Dish))
            .HasValue<Combo>(nameof(Combo));
        builder.Property<string>(DiscriminatorColumn)
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(item => item.Name)
            .HasMaxLength(MenuItem.NameMaxLength);

        builder.Property(item => item.IsAvailable);
        builder.Property(item => item.IsTwoForOneEligible);

        // Calculated (recursive for combos) or in-memory only: not columns.
        builder.Ignore(item => item.CanBeOrdered);
        builder.Ignore(item => item.DomainEvents);
    }
}
