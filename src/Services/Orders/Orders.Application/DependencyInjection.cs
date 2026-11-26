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

        // Mediator: registers every IRequestHandler in this assembly, so a new use case needs no extra line here
        // (Open/Closed). Handlers are transient and receive the request's scoped repositories and Unit of Work.
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
