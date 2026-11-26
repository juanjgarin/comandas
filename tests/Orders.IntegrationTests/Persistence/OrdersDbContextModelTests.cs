using Comandas.Orders.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Comandas.Orders.IntegrationTests.Persistence;

/// <summary>
/// Checks the EF Core model WITHOUT a database: building the model validates every configuration,
/// and the generated CREATE script shows exactly which tables and columns the mapping produces.
/// </summary>
public sealed class OrdersDbContextModelTests
{
    private static readonly string CreateScript = GenerateCreateScript();

    [Theory]
    [InlineData("[Orders]")]
    [InlineData("[OrderLines]")]
    [InlineData("[MenuItems]")]
    [InlineData("[ComboComponents]")]
    public void Model_CreatesExpectedTables(string table)
    {
        CreateScript.Should().Contain($"CREATE TABLE {table}");
    }

    [Fact]
    public void Model_MapsMenuHierarchyAsTphWithDiscriminator()
    {
        CreateScript.Should().NotContain("CREATE TABLE [Dishes]").And.NotContain("CREATE TABLE [Combos]");
        CreateScript.Should().Contain("[Kind] varchar(20) NOT NULL");
    }

    [Fact]
    public void Model_StoresMoneyAsOwnedColumnsInsideOwnerTable()
    {
        CreateScript.Should().Contain("[UnitPrice] decimal(18,2) NOT NULL");
        CreateScript.Should().Contain("[UnitPriceCurrency] char(3) NOT NULL");
        CreateScript.Should().Contain("[BasePrice] decimal(18,2) NULL", "TPH: combo rows have no base price");
    }

    [Fact]
    public void Model_StoresStatusAsTextAndHasRowVersion()
    {
        CreateScript.Should().Contain("[Status] varchar(20) NOT NULL");
        CreateScript.Should().Contain("[RowVersion] rowversion", "SQL Server fills it on every insert/update");
    }

    [Theory]
    [InlineData("[OrderId] uniqueidentifier NOT NULL")]
    [InlineData("[ComboId] uniqueidentifier NOT NULL")]
    public void Model_ChildRowsCannotBeOrphans(string requiredForeignKey)
    {
        CreateScript.Should().Contain(requiredForeignKey);
    }

    [Fact]
    public void Model_OrderLinesSnapshotTheTwoForOneFlag()
    {
        var orderLinesTable = CreateScript[CreateScript.IndexOf("CREATE TABLE [OrderLines]", StringComparison.Ordinal)..];

        orderLinesTable.Should().Contain("[IsTwoForOneEligible] bit NOT NULL");
    }

    [Fact]
    public void Model_StoresQuantityAsInt()
    {
        CreateScript.Should().Contain("[Quantity] int NOT NULL");
    }

    private static string GenerateCreateScript()
    {
        // The connection string is never opened: generating the script only needs the provider.
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options;

        using var context = new OrdersDbContext(options);
        return context.Database.GenerateCreateScript();
    }
}
