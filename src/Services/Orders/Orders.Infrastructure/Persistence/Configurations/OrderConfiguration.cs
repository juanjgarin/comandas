using Comandas.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comandas.Orders.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public const string RowVersionProperty = "RowVersion";

    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);

        // The domain creates the Guid (Guid.CreateVersion7()). Without this, EF would assume a non-empty key
        // means "already in the database" and try an UPDATE instead of an INSERT.
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.TableNumber);

        builder.Property(order => order.Currency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsUnicode(false);

        // State pattern in memory, a readable string in the database ("Confirmed", "Ready"...).
        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsUnicode(false);

        // Optimistic concurrency as a SHADOW property: the column exists, the domain class does not know about it.
        // SQL Server changes it on every UPDATE; a stale value makes SaveChanges throw DbUpdateConcurrencyException.
        builder.Property<byte[]>(RowVersionProperty).IsRowVersion();

        // Lines belong to the aggregate: deleted with it, and populated through the private _lines field
        // so the public IReadOnlyCollection never needs a setter.
        // IsRequired: a shadow foreign key is optional by default, which would allow orphan lines (OrderId NULL).
        builder.HasMany(order => order.Lines)
            .WithOne()
            .HasForeignKey("OrderId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Calculated or in-memory only: not columns.
        builder.Ignore(order => order.Subtotal);
        builder.Ignore(order => order.DomainEvents);
    }
}
