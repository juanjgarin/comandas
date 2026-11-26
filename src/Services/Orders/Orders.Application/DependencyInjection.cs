using Comandas.Orders.Application.Menu;
using Comandas.Orders.Application.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace Comandas.Orders.Application;

/// <summary>Registers this layer's services. Each layer owns its registrations; the API only calls them.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Scoped: one instance per HTTP request, sharing that request's repositories and Unit of Work.
        services.AddScoped<OrderApplicationService>();
        services.AddScoped<MenuApplicationService>();

        return services;
    }
}
