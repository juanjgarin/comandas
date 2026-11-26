namespace Comandas.Orders.Domain.Orders;

/// <summary>
/// The lifecycle position of an order, as stored in the database and shown to clients.
/// The behavior of each status lives in the matching <c>OrderState</c> class (State pattern).
/// </summary>
public enum OrderStatus
{
    Created,
    Confirmed,
    InPreparation,
    Ready,
    Delivered,
    Cancelled,
}
