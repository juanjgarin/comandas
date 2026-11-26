using Comandas.Orders.Application.Menu;
using Microsoft.AspNetCore.Mvc;

namespace Comandas.Orders.API.Controllers;

/// <summary>Read-only menu for the waiter view.</summary>
[ApiController]
[Route("api/menu")]
[Produces("application/json")]
public sealed class MenuController(MenuApplicationService menu) : ControllerBase
{
    /// <summary>Every menu item (dishes and combos) with its current price and availability.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MenuItemDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetMenu(CancellationToken cancellationToken) =>
        Ok(await menu.GetMenuAsync(cancellationToken));
}
