using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.Orders.Infrastructure.Persistence;
using Comandas.Orders.Infrastructure.Persistence.Repositories;
using Comandas.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Comandas.Orders.IntegrationTests.Persistence;

/// <summary>Round-trips the Order aggregate through the real repository, Unit of Work and SQL Server.</summary>
public sealed class OrderPersistenceTests(SqlServerFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Migrations_AreApplied()
    {
        await using var context = database.CreateContext();

        var applied = await context.Database.GetAppliedMigrationsAsync(Ct);

        applied.Should().ContainSingle(migration => migration.EndsWith("_InitialCreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SaveAndLoad_OrderWithLines_RoundTripsAllValues()
    {
        var burger = Dish.Create("Burger", Money.Of(10m, "EUR"));
        var soda = Dish.Create("Soda", Money.Of(2.50m, "EUR"));
        soda.EnableTwoForOne();
        var order = Order.Create(12, "EUR");
        order.AddItem(burger, Quantity.Of(2));
        order.AddItem(soda, Quantity.Of(3));
        await SaveNewAsync(order);

        // Read with a DIFFERENT context: the order must come from SQL Server, not from EF's memory.
        await using var readContext = database.CreateContext();
        var loaded = await new OrderRepository(readContext).GetByIdAsync(order.Id, Ct);

        loaded.Should().NotBeNull();
        loaded!.TableNumber.Should().Be(12);
        loaded.Currency.Should().Be("EUR");
        loaded.Status.Should().Be(OrderStatus.Created);
        loaded.Subtotal.Should().Be(Money.Of(27.50m, "EUR"));
        loaded.Lines.Should().BeEquivalentTo(
            [
                new { ItemName = "Burger", UnitPrice = Money.Of(10m, "EUR"), Quantity = Quantity.Of(2), IsTwoForOneEligible = false },
                new { ItemName = "Soda", UnitPrice = Money.Of(2.50m, "EUR"), Quantity = Quantity.Of(3), IsTwoForOneEligible = true },
            ],
            options => options.ExcludingMissingMembers());
        loaded.DomainEvents.Should().BeEmpty("events are not persisted; a loaded aggregate starts clean");
    }

    [Fact]
    public async Task AddItem_ToExistingOrder_InsertsTheNewLine()
    {
        // Guards the ValueGeneratedNever() configuration: a new line with a domain-generated Guid must be
        // INSERTed. Without it, EF would assume the line already exists and try an UPDATE that hits 0 rows.
        var order = Order.Create(3, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.One);
        await SaveNewAsync(order);

        await using (var context = database.CreateContext())
        {
            var tracked = await new OrderRepository(context).GetByIdAsync(order.Id, Ct);
            tracked!.AddItem(Dish.Create("Fries", Money.Of(3.50m, "EUR")), Quantity.One);
            await new EfUnitOfWork(context).SaveChangesAsync(Ct);
        }

        await using var readContext = database.CreateContext();
        var reloaded = await new OrderRepository(readContext).GetByIdAsync(order.Id, Ct);
        reloaded!.Lines.Select(line => line.ItemName).Should().BeEquivalentTo("Burger", "Fries");
    }

    [Fact]
    public async Task StatusTransition_IsPersistedAsText()
    {
        var order = Order.Create(5, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.One);
        await SaveNewAsync(order);

        await using (var context = database.CreateContext())
        {
            var tracked = await new OrderRepository(context).GetByIdAsync(order.Id, Ct);
            tracked!.Confirm();
            await new EfUnitOfWork(context).SaveChangesAsync(Ct);
        }

        await using var readContext = database.CreateContext();
        // Raw SQL on purpose: we want the column as SQL Server stores it, not as EF converts it back.
        var storedStatus = await readContext.Database
            .SqlQuery<string>($"SELECT [Status] AS [Value] FROM [Orders] WHERE [Id] = {order.Id}")
            .SingleAsync(Ct);
        storedStatus.Should().Be("Confirmed");
    }

    [Fact]
    public async Task RemoveLine_DeletesTheRow()
    {
        var order = Order.Create(8, "EUR");
        var line = order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.One);
        await SaveNewAsync(order);

        await using (var context = database.CreateContext())
        {
            var tracked = await new OrderRepository(context).GetByIdAsync(order.Id, Ct);
            tracked!.RemoveLine(line.Id);
            await new EfUnitOfWork(context).SaveChangesAsync(Ct);
        }

        await using var readContext = database.CreateContext();
        var lineRows = await readContext.Set<OrderLine>().CountAsync(l => l.Id == line.Id, Ct);
        lineRows.Should().Be(0);
    }

    [Fact]
    public async Task GetById_UnknownOrder_ReturnsNull()
    {
        await using var context = database.CreateContext();

        var loaded = await new OrderRepository(context).GetByIdAsync(Guid.NewGuid(), Ct);

        loaded.Should().BeNull();
    }

    private async Task SaveNewAsync(Order order)
    {
        await using var context = database.CreateContext();
        new OrderRepository(context).Add(order);
        await new EfUnitOfWork(context).SaveChangesAsync(Ct);
    }
}
