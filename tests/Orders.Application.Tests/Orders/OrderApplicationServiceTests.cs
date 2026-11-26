using Comandas.Orders.Application.Common;
using Comandas.Orders.Application.Orders;
using Comandas.Orders.Application.Tests.Fakes;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Application.Tests.Orders;

/// <summary>The read side that is left in the application service until step 2.8 turns it into a query.</summary>
public sealed class OrderApplicationServiceTests
{
    private readonly InMemoryOrderRepository _orders = new();
    private readonly OrderApplicationService _service;

    public OrderApplicationServiceTests()
    {
        _service = new OrderApplicationService(_orders);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task GetOrderAsync_ReturnsDtoWithLinesAndTotals()
    {
        var burger = Dish.Create("Burger", Money.Of(10m, "EUR"));
        var order = Order.Create(3, "EUR");
        order.AddItem(burger, Quantity.Of(2));
        _orders.Add(order);

        var dto = await _service.GetOrderAsync(order.Id, Ct);

        dto.Id.Should().Be(order.Id);
        dto.TableNumber.Should().Be(3);
        dto.Status.Should().Be("Created");
        dto.Currency.Should().Be("EUR");
        dto.Subtotal.Should().Be(20m);
        dto.DiscountDescription.Should().BeNull();
        dto.DiscountAmount.Should().Be(0m);
        dto.Total.Should().Be(20m);
        dto.Lines.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            MenuItemId = burger.Id,
            ItemName = "Burger",
            UnitPrice = 10m,
            Quantity = 2,
            LineTotal = 20m,
        });
    }

    [Fact]
    public async Task GetOrderAsync_UnknownOrder_ThrowsNotFound()
    {
        var act = () => _service.GetOrderAsync(Guid.NewGuid(), Ct);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Order *");
    }
}
