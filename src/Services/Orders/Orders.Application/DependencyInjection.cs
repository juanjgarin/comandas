using Comandas.Orders.Application.Common;
using Comandas.Orders.Application.Menu;
using Comandas.Orders.Application.Orders;
using Comandas.Orders.Domain.Discounts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

        // Discounts: the clock is a dependency (the real one here, a fake one in tests). TryAdd lets the host
        // provide its own TimeProvider first.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IValidateOptions<DiscountOptions>, DiscountOptionsValidator>();
        services.AddOptions<DiscountOptions>().ValidateOnStart();

        // Singleton: the factory is stateless between calls (it only holds the clock and the configured strategies).
        services.AddSingleton<IDiscountFactory>(provider => new DiscountFactory(
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<IOptions<DiscountOptions>>().Value.ToPolicy()));

        return services;
    }
}
