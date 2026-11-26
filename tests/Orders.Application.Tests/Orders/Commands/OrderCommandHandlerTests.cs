using Comandas.Orders.Application.Common;
using Comandas.Orders.Application.Orders.Commands.CancelOrder;
using Comandas.Orders.Application.Orders.Commands.ChangeOrderLineQuantity;
using Comandas.Orders.Application.Orders.Commands.ConfirmOrder;
using Comandas.Orders.Application.Orders.Commands.DeliverOrder;
using Comandas.Orders.Application.Orders.Commands.MarkOrderReady;
using Comandas.Orders.Application.Orders.Commands.RemoveOrderLine;
using Comandas.Orders.Application.Orders.Commands.StartOrderPreparation;
using Comandas.Orders.Application.Tests.Fakes;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Application.Tests.Orders.Commands;

/// <summary>
/// The handlers built on the Template Method base: the shared script (load or not found, act, save once) is
/// tested through one of them; each concrete handler only needs a test that it calls the right domain method.
/// </summary>
public sealed class OrderCommandHandlerTests
{
    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // --- The template's script ---

    [Fact]
    public async Task Handle_ValidOperation_ActsAndSavesOnce()
    {
        var order = ExistingOrderWithOneBurger();

        await new ConfirmOrderCommandHandler(_orders, _unitOfWork).Handle(new ConfirmOrderCommand(order.Id), Ct);

        order.Status.Should().Be(OrderStatus.Confirmed);
        _unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ThrowsNotFoundAndDoesNotSave()
    {
        var act = () => new ConfirmOrderCommandHandler(_orders, _unitOfWork).Handle(new ConfirmOrderCommand(Guid.NewGuid()), Ct);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Order *");
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DomainRejectsOperation_PropagatesDomainExceptionAndDoesNotSave()
    {
        var emptyOrder = Order.Create(1, "EUR");
        _orders.Add(emptyOrder);

        var act = () => new ConfirmOrderCommandHandler(_orders, _unitOfWork).Handle(new ConfirmOrderCommand(emptyOrder.Id), Ct);

        await act.Should().ThrowAsync<DomainException>();
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    // --- Each concrete handler calls its domain method ---

    [Fact]
    public async Task TransitionHandlers_FullFlow_ReachDelivered()
    {
        var order = ExistingOrderWithOneBurger();

        await new ConfirmOrderCommandHandler(_orders, _unitOfWork).Handle(new ConfirmOrderCommand(order.Id), Ct);
        await new StartOrderPreparationCommandHandler(_orders, _unitOfWork).Handle(new StartOrderPreparationCommand(order.Id), Ct);
        await new MarkOrderReadyCommandHandler(_orders, _unitOfWork).Handle(new MarkOrderReadyCommand(order.Id), Ct);
        await new DeliverOrderCommandHandler(_orders, _unitOfWork).Handle(new DeliverOrderCommand(order.Id), Ct);

        order.Status.Should().Be(OrderStatus.Delivered);
        _unitOfWork.SaveChangesCalls.Should().Be(4);
    }

    [Fact]
    public async Task CancelOrderHandler_CreatedOrder_Cancels()
    {
        var order = ExistingOrderWithOneBurger();

        await new CancelOrderCommandHandler(_orders, _unitOfWork).Handle(new CancelOrderCommand(order.Id), Ct);

        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public async Task ChangeOrderLineQuantityHandler_ExistingLine_ChangesQuantity()
    {
        var order = ExistingOrderWithOneBurger();
        var lineId = order.Lines.Single().Id;

        await new ChangeOrderLineQuantityCommandHandler(_orders, _unitOfWork)
            .Handle(new ChangeOrderLineQuantityCommand(order.Id, lineId, 4), Ct);

        order.Lines.Single().Quantity.Should().Be(Quantity.Of(4));
    }

    [Fact]
    public async Task RemoveOrderLineHandler_ExistingLine_RemovesIt()
    {
        var order = ExistingOrderWithOneBurger();
        var lineId = order.Lines.Single().Id;

        await new RemoveOrderLineCommandHandler(_orders, _unitOfWork).Handle(new RemoveOrderLineCommand(order.Id, lineId), Ct);

        order.Lines.Should().BeEmpty();
    }

    private Order ExistingOrderWithOneBurger()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.One);
        _orders.Add(order);
        return order;
    }
}
