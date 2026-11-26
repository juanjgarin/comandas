using Comandas.Orders.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(Comandas.Orders.IntegrationTests.SqlServerFixture))]

namespace Comandas.Orders.IntegrationTests;

/// <summary>
/// ONE disposable SQL Server for the whole test run (an xUnit assembly fixture): started once, migrated with
/// the REAL migrations, removed at the end. Tests isolate themselves by creating their own data (new Guids).
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Same image as local development (already downloaded), so tests run against the same engine.
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest")
        .WithLabel(TestcontainersConfiguration.ContainerLabel, "true")
        .Build();

    private string _connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // The container's connection string points to "master"; use a dedicated database instead.
        _connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "OrdersTests",
        }.ConnectionString;

        await using var context = CreateContext();
        await context.Database.MigrateAsync(); // the real migrations, not EnsureCreated
    }

    /// <summary>
    /// A NEW context each time. Tests write with one context and read with another: reading with the same one
    /// would return the objects EF already holds in memory and prove nothing about what was saved.
    /// </summary>
    public OrdersDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<OrdersDbContext>().UseSqlServer(_connectionString).Options);

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
