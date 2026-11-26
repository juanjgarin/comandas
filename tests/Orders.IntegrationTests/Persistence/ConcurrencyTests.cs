using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.Orders.Infrastructure.Persistence;
using Comandas.Orders.Infrastructure.Persistence.Repositories;
using Comandas.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Comandas.Orders.IntegrationTests.Persistence;

/// <summary>
/// Optimistic concurrency with RowVersion: two users load the same order; the first save wins and the
/// second gets DbUpdateConcurrencyException instead of silently overwriting (a "lost update").
/// </summary>
public sealed class ConcurrencyTests(SqlServerFixture database)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ConflictingStatusChanges_SecondSaveThrowsConcurrencyException()
    {
        var order = Order.Create(21, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.One);
        await using (var context = database.CreateContext())
        {
            new OrderRepository(context).Add(order);
            await new EfUnitOfWork(context).SaveChangesAsync(Ct);
        }

        // Waiter A and waiter B both open the same order (same RowVersion).
        await using var contextA = database.CreateContext();
        await using var contextB = database.CreateContext();
        var orderSeenByA = await new OrderRepository(contextA).GetByIdAsync(order.Id, Ct);
        var orderSeenByB = await new OrderRepository(contextB).GetByIdAsync(order.Id, Ct);

        // A confirms first: SQL Server bumps the RowVersion.
        orderSeenByA!.Confirm();
        await new EfUnitOfWork(contextA).SaveChangesAsync(Ct);

        // B, still holding the old RowVersion, tries to cancel: rejected.
        orderSeenByB!.Cancel();
        var act = () => new EfUnitOfWork(contextB).SaveChangesAsync(Ct);

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();

        await using var readContext = database.CreateContext();
        var stored = await new OrderRepository(readContext).GetByIdAsync(order.Id, Ct);
        stored!.Status.Should().Be(OrderStatus.Confirmed, "the first save won; nothing was silently overwritten");
    }
}
