using Comandas.Orders.Application.Common;
using Comandas.Orders.Application.Orders.Commands.CreateOrder;
using Comandas.Orders.Application.Tests.Fakes;
using Comandas.SharedKernel.Domain;
using Microsoft.Extensions.Options;

namespace Comandas.Orders.Application.Tests.Orders.Commands;

/// <summary>
/// The handler is a plain class: tested by calling Handle directly, without MediatR. The Mediator only
/// routes the message; the use case itself has no framework to set up.
/// </summary>
public sealed class CreateOrderCommandHandlerTests
{
    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _handler = new CreateOrderCommandHandler(
            _orders, _unitOfWork, Options.Create(new RestaurantOptions { Currency = "EUR" }));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_ValidTable_AddsOrderWithConfiguredCurrencyAndSavesOnce()
    {
        var orderId = await _handler.Handle(new CreateOrderCommand(4), Ct);

        var order = _orders.All.Should().ContainSingle().Subject;
        order.Id.Should().Be(orderId);
        order.TableNumber.Should().Be(4);
        order.Currency.Should().Be("EUR");
        _unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_InvalidTable_PropagatesDomainExceptionAndDoesNotSave()
    {
        var act = () => _handler.Handle(new CreateOrderCommand(0), Ct);

        await act.Should().ThrowAsync<DomainException>();
        _orders.All.Should().BeEmpty();
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }
}
