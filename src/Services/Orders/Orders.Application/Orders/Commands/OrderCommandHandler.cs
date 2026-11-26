using Comandas.Orders.Application.Abstractions;
using Comandas.Orders.Domain.Orders;
using MediatR;

namespace Comandas.Orders.Application.Orders.Commands;

/// <summary>
/// Template Method pattern: the fixed script of every "act on an existing order" command lives here
/// (load or not found, let the domain act, save once); each concrete handler fills in only the step that varies,
/// <see cref="Execute"/>. One command per intention, without repeating the plumbing in each handler.
/// </summary>
public abstract class OrderCommandHandler<TCommand>(IOrderRepository orders, IUnitOfWork unitOfWork)
    : IRequestHandler<TCommand>
    where TCommand : IOrderCommand
{
    public async Task Handle(TCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var order = await orders.GetExistingAsync(request.OrderId, cancellationToken);

        // If the domain rejects the operation it throws, and nothing is saved.
        Execute(order, request);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The variable step: ask the aggregate to do the work. No I/O here.</summary>
    protected abstract void Execute(Order order, TCommand command);
}
