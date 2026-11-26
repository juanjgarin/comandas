using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Infrastructure.Persistence;
using Comandas.Orders.Infrastructure.Persistence.Repositories;
using Comandas.Orders.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Comandas.Orders.Infrastructure;

/// <summary>
/// Registers the Infrastructure implementations behind the Application interfaces.
/// This is where Dependency Inversion is "closed": IOrderRepository -> OrderRepository, etc.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<OrdersDbContext>(options => options
            .UseSqlServer(connectionString)
            // EF Core 9+ seeding hooks: run by Migrate() after applying migrations. Both are registered
            // because EF tooling uses the synchronous one and the app uses the asynchronous one.
            .UseSeeding((context, _) => MenuSeeder.Seed((OrdersDbContext)context))
            .UseAsyncSeeding((context, _, cancellationToken) =>
                MenuSeeder.SeedAsync((OrdersDbContext)context, cancellationToken)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        return services;
    }

    /// <summary>
    /// Applies pending migrations (and then the seeding hooks) on startup. Fine for development and this
    /// project; production systems usually apply migrations in the deployment pipeline instead.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        await context.Database.MigrateAsync(cancellationToken);
    }
}
