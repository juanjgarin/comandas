using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Application.Common;
using Comandas.Orders.Domain.Orders;
using MediatR;
using Microsoft.Extensions.Options;

namespace Comandas.Orders.Application.Orders.Commands.CreateOrder;

/// <summary>
/// One handler, one use case (Single Responsibility): it depends only on what creating an order needs,
/// unlike the old application service, which received every dependency of every use case.
/// MediatR finds it by its <see cref="IRequestHandler{TRequest,TResponse}"/> interface; the caller never
/// references this class (Mediator pattern).
/// </summary>
public sealed class CreateOrderCommandHandler(
    IOrderRepository orders,
    IUnitOfWork unitOfWork,
    IOptions<RestaurantOptions> restaurantOptions) : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = Order.Create(request.TableNumber, restaurantOptions.Value.Currency);

        orders.Add(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
