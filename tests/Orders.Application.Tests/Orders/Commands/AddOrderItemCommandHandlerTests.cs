using Comandas.Orders.Application.Common;
using Comandas.Orders.Application.Orders.Commands.AddOrderItem;
using Comandas.Orders.Application.Tests.Fakes;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Application.Tests.Orders.Commands;

public sealed class AddOrderItemCommandHandlerTests
{
    private readonly Dish _burger = Dish.Create("Burger", Money.Of(10m, "EUR"));
    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly AddOrderItemCommandHandler _handler;

    public AddOrderItemCommandHandlerTests()
    {
        _handler = new AddOrderItemCommandHandler(_orders, new InMemoryMenuItemRepository(_burger), _unitOfWork);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_ValidItem_AddsLineAndSavesOnce()
    {
        var order = ExistingOrder();

        var lineId = await _handler.Handle(new AddOrderItemCommand(order.Id, _burger.Id, 2), Ct);

        var line = order.Lines.Should().ContainSingle().Subject;
        line.Id.Should().Be(lineId);
        line.Quantity.Should().Be(Quantity.Of(2));
        _unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundAndDoesNotSave()
    {
        var act = () => _handler.Handle(new AddOrderItemCommand(Guid.NewGuid(), _burger.Id, 1), Ct);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Order *");
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_UnknownMenuItem_ThrowsNotFoundAndDoesNotSave()
    {
        var order = ExistingOrder();

        var act = () => _handler.Handle(new AddOrderItemCommand(order.Id, Guid.NewGuid(), 1), Ct);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Menu item *");
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InvalidQuantity_PropagatesDomainExceptionAndDoesNotSave()
    {
        var order = ExistingOrder();

        var act = () => _handler.Handle(new AddOrderItemCommand(order.Id, _burger.Id, 0), Ct);

        await act.Should().ThrowAsync<DomainException>();
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    private Order ExistingOrder()
    {
        var order = Order.Create(1, "EUR");
        _orders.Add(order);
        return order;
    }
}
