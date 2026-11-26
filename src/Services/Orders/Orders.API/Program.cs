using Comandas.Orders.Application;
using Comandas.Orders.Application.Common;
using Comandas.Orders.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// --- Composition root: the only place that knows every layer and wires them together. ---
builder.Services.Configure<RestaurantOptions>(builder.Configuration.GetSection(RestaurantOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("OrdersDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "Connection string 'OrdersDb' is missing. For local development set it with: " +
        "dotnet user-secrets set \"ConnectionStrings:OrdersDb\" \"<connection string>\" --project src/Services/Orders/Orders.API");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

// Creates/updates the database schema and seeds the sample menu.
await app.Services.ApplyMigrationsAsync();

// Placeholder until the real endpoints arrive in step 1.15.
app.MapGet("/", () => "Orders.API is running");

await app.RunAsync();
