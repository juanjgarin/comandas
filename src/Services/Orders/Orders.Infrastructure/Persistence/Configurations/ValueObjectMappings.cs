using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Comandas.Orders.Infrastructure.Persistence.Configurations;

/// <summary>
/// How the shared value objects are stored, defined once and reused by every configuration (DRY).
/// Two techniques on purpose:
/// <list type="bullet">
/// <item><see cref="Money"/> has two values -> OWNED TYPE: its columns live inside the owner's table.</item>
/// <item><see cref="Quantity"/> has one value -> VALUE CONVERTER: stored as a plain int column.</item>
/// </list>
/// </summary>
internal static class ValueObjectMappings
{
    public static readonly ValueConverter<Quantity, int> QuantityConverter =
        new(quantity => quantity.Value, value => Quantity.Of(value));

    /// <summary>Maps a <see cref="Money"/> owned type to "{prefix}" (amount) and "{prefix}Currency" columns.</summary>
    public static void MapMoney<TOwner>(this OwnedNavigationBuilder<TOwner, Money> money, string columnPrefix)
        where TOwner : class
    {
        money.Property(m => m.Amount)
            .HasColumnName(columnPrefix)
            .HasPrecision(18, Money.DecimalPlaces);

        money.Property(m => m.Currency)
            .HasColumnName($"{columnPrefix}Currency")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsUnicode(false);
    }
}
