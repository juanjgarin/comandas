using Comandas.Orders.API.Contracts;
using Comandas.Orders.Application.Orders;
using Microsoft.AspNetCore.Mvc;

namespace Comandas.Orders.API.Controllers;

/// <summary>
/// Thin HTTP adapter over the order use cases: translate HTTP -> use case -> HTTP status, nothing else.
/// No business rules here; they live in the domain and are orchestrated by the application service.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public sealed class OrdersController(OrderApplicationService orders) : ControllerBase
{
    /// <summary>Opens a new order for a table.</summary>
    [HttpPost]
    [ProducesResponseType<CreatedResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<CreatedResponse>> Create(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var orderId = await orders.CreateOrderAsync(request.TableNumber, cancellationToken);

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

        var lineId = await orders.AddItemAsync(id, request.MenuItemId, request.Quantity, cancellationToken);
        return Ok(new AddItemResponse(lineId));
    }

    [HttpPatch("{id:guid}/items/{lineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeLineQuantity(
        Guid id, Guid lineId, ChangeLineQuantityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await orders.ChangeLineQuantityAsync(id, lineId, request.Quantity, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/items/{lineId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveLine(Guid id, Guid lineId, CancellationToken cancellationToken)
    {
        await orders.RemoveLineAsync(id, lineId, cancellationToken);
        return NoContent();
    }

    // --- Lifecycle transitions (State pattern in the domain). Actions, not field updates: POST to a verb. ---

    [HttpPost("{id:guid}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        await orders.ConfirmAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/start-preparation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> StartPreparation(Guid id, CancellationToken cancellationToken)
    {
        await orders.StartPreparationAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-ready")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkReady(Guid id, CancellationToken cancellationToken)
    {
        await orders.MarkReadyAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deliver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Deliver(Guid id, CancellationToken cancellationToken)
    {
        await orders.DeliverAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await orders.CancelAsync(id, cancellationToken);
        return NoContent();
    }
}
