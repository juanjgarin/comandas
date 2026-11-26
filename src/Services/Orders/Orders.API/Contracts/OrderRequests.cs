namespace Comandas.Orders.API.Contracts;

// HTTP request/response bodies: the shape of the API, kept apart from the use cases they feed.
// Validation of their contents arrives in step 2.9 (FluentValidation); today the domain rejects bad values.

public sealed record CreateOrderRequest(int TableNumber);

public sealed record AddItemRequest(Guid MenuItemId, int Quantity);

public sealed record ChangeLineQuantityRequest(int Quantity);

public sealed record CreatedResponse(Guid Id);

public sealed record AddItemResponse(Guid LineId);
