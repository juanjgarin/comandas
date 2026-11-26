using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace Comandas.Orders.Infrastructure.Persistence;

/// <summary>
/// EF Core session over the Orders database. Mapping lives in one IEntityTypeConfiguration per entity
/// (see Configurations/), not in the domain classes, which stay persistence-ignorant.
/// </summary>
public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Picks up every IEntityTypeConfiguration<T> in this assembly.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
