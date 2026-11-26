using Comandas.Orders.API.Contracts;
using Comandas.Orders.Application.Orders;
using Comandas.Orders.Application.Orders.Commands.AddOrderItem;
using Comandas.Orders.Application.Orders.Commands.ApplyOrderDiscount;
using Comandas.Orders.Application.Orders.Commands.CancelOrder;
using Comandas.Orders.Application.Orders.Commands.ChangeOrderLineQuantity;
using Comandas.Orders.Application.Orders.Commands.ConfirmOrder;
using Comandas.Orders.Application.Orders.Commands.CreateOrder;
using Comandas.Orders.Application.Orders.Commands.DeliverOrder;
using Comandas.Orders.Application.Orders.Commands.MarkOrderReady;
using Comandas.Orders.Application.Orders.Commands.RemoveOrderLine;
using Comandas.Orders.Application.Orders.Commands.StartOrderPreparation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Comandas.Orders.API.Controllers;

/// <summary>
/// Thin HTTP adapter over the order use cases: translate HTTP -> command -> HTTP status, nothing else.
/// No business rules here; they live in the domain and are orchestrated by the application layer.
/// </summary>
/// <remarks>
/// Every write is a CQRS command sent through MediatR's <see cref="ISender"/>: the controller does not know
/// which class handles it (Mediator). The read still uses the application service until step 2.8.
/// The HTTP request bodies (API contract) and the commands (application contract) are separate types on purpose:
/// the public API can evolve without changing the use cases, and vice versa.
/// </remarks>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController(ISender sender, OrderApplicationService orders) : ControllerBase
{
    /// <summary>Opens a new order for a table.</summary>
    [HttpPost]
    [ProducesResponseType<CreatedResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderId = await sender.Send(new CreateOrderCommand(request.TableNumber), cancellationToken);

        // 201 + Location header pointing to GET /api/orders/{id}
        return CreatedAtAction(nameof(GetById), new { id = orderId }, new CreatedResponse(orderId));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await orders.GetOrderAsync(id, cancellationToken));

    /// <summary>Adds a menu item; if the same item at the same price is already there, its quantity grows.</summary>
    [HttpPost("{id:guid}/items")]
    [ProducesResponseType<AddItemResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AddItemResponse>> AddItem(Guid id, AddItemRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var lineId = await sender.Send(new AddOrderItemCommand(id, request.MenuItemId, request.Quantity), cancellationToken);
        return Ok(new AddItemResponse(lineId));
    }

    [HttpPatch("{id:guid}/items/{lineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeLineQuantity(
        Guid id, Guid lineId, ChangeLineQuantityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await sender.Send(new ChangeOrderLineQuantityCommand(id, lineId, request.Quantity), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/items/{lineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        await sender.Send(new RemoveOrderLineCommand(id, lineId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Applies the best discount available now (happy hour, 2-for-1 or the optional manual percentage).
    /// Any later change to the lines removes it; apply it again after editing.
    /// </summary>
    [HttpPost("{id:guid}/discount")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ApplyDiscount(Guid id, ApplyDiscountRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await sender.Send(new ApplyOrderDiscountCommand(id, request.ManualPercentage), cancellationToken);
        return NoContent();
    }

    // --- Lifecycle transitions (State pattern in the domain). Actions, not field updates: POST to a verb. ---

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken) =>
        SendAsync(new ConfirmOrderCommand(id), cancellationToken);

    [HttpPost("{id:guid}/start-preparation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> StartPreparation(Guid id, CancellationToken cancellationToken) =>
        SendAsync(new StartOrderPreparationCommand(id), cancellationToken);

    [HttpPost("{id:guid}/mark-ready")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> MarkReady(Guid id, CancellationToken cancellationToken) =>
        SendAsync(new MarkOrderReadyCommand(id), cancellationToken);

    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken) =>
        SendAsync(new DeliverOrderCommand(id), cancellationToken);

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken) =>
        SendAsync(new CancelOrderCommand(id), cancellationToken);

    private async Task<IActionResult> SendAsync(IRequest command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }
}
