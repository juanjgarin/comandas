using Comandas.Orders.Application.Common;
using Comandas.Orders.Application.Orders;
using Comandas.Orders.Application.Tests.Fakes;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Application.Tests.Orders;

public sealed class OrderApplicationServiceTests
{
    private readonly Dish _burger = Dish.Create("Burger", Money.Of(10m, "EUR"));
    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly OrderApplicationService _service;

    public OrderApplicationServiceTests()
    {
        _service = new OrderApplicationService(_orders, new InMemoryMenuItemRepository(_burger), _unitOfWork);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task AddItemAsync_ValidItem_AddsLineAndSaves()
    {
        var orderId = ExistingOrder();

        var lineId = await _service.AddItemAsync(orderId, _burger.Id, 2, Ct);

        var line = _orders.All.Single().Lines.Should().ContainSingle().Subject;
        line.Id.Should().Be(lineId);
        line.Quantity.Value.Should().Be(2);
        _unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task AddItemAsync_UnknownOrder_ThrowsNotFoundAndDoesNotSave()
    {
        var act = () => _service.AddItemAsync(Guid.NewGuid(), _burger.Id, 1, Ct);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Order *");
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task AddItemAsync_UnknownMenuItem_ThrowsNotFound()
    {
        var orderId = ExistingOrder();

        var act = () => _service.AddItemAsync(orderId, Guid.NewGuid(), 1, Ct);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Menu item *");
    }

    [Fact]
    public async Task AddItemAsync_InvalidQuantity_PropagatesDomainException()
    {
        var orderId = ExistingOrder();

        var act = () => _service.AddItemAsync(orderId, _burger.Id, 0, Ct);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task ConfirmAsync_OrderWithLines_ConfirmsAndSaves()
    {
        var orderId = ExistingOrder();
        await _service.AddItemAsync(orderId, _burger.Id, 1, Ct);

        await _service.ConfirmAsync(orderId, Ct);

        _orders.All.Single().Status.Should().Be(OrderStatus.Confirmed);
        _unitOfWork.SaveChangesCalls.Should().Be(2, "one save for adding, one for confirming");
    }

    [Fact]
    public async Task ConfirmAsync_EmptyOrder_PropagatesDomainExceptionAndDoesNotSave()
    {
        var orderId = ExistingOrder();

        var act = () => _service.ConfirmAsync(orderId, Ct);

        await act.Should().ThrowAsync<DomainException>();
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task LifecycleMethods_FullFlow_ReachDelivered()
    {
        var orderId = ExistingOrder();
        await _service.AddItemAsync(orderId, _burger.Id, 1, Ct);

        await _service.ConfirmAsync(orderId, Ct);
        await _service.StartPreparationAsync(orderId, Ct);
        await _service.MarkReadyAsync(orderId, Ct);
        await _service.DeliverAsync(orderId, Ct);

        _orders.All.Single().Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public async Task GetOrderAsync_ReturnsDtoWithLinesAndTotals()
    {
        var orderId = ExistingOrder(tableNumber: 3);
        await _service.AddItemAsync(orderId, _burger.Id, 2, Ct);

        var dto = await _service.GetOrderAsync(orderId, Ct);

        dto.Id.Should().Be(orderId);
        dto.TableNumber.Should().Be(3);
        dto.Status.Should().Be("Created");
        dto.Currency.Should().Be("EUR");
        dto.Subtotal.Should().Be(20m);
        dto.DiscountDescription.Should().BeNull();
        dto.DiscountAmount.Should().Be(0m);
        dto.Total.Should().Be(20m);
        dto.Lines.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            MenuItemId = _burger.Id,
            ItemName = "Burger",
            UnitPrice = 10m,
            Quantity = 2,
            LineTotal = 20m,
        });
    }

    [Fact]
    public async Task RemoveLineAsync_ExistingLine_RemovesItAndSaves()
    {
        var orderId = ExistingOrder();
        var lineId = await _service.AddItemAsync(orderId, _burger.Id, 1, Ct);

        await _service.RemoveLineAsync(orderId, lineId, Ct);

        _orders.All.Single().Lines.Should().BeEmpty();
        _unitOfWork.SaveChangesCalls.Should().Be(2);
    }

    /// <summary>Arrange helper: the order already exists (creating it is CreateOrderCommandHandler's job).</summary>
    private Guid ExistingOrder(int tableNumber = 1)
    {
        var order = Order.Create(tableNumber, "EUR");
        _orders.Add(order);
        return order.Id;
    }
}
