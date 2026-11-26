using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Orders.States;

/// <summary>
/// State pattern: one subclass per <see cref="OrderStatus"/>. Each state overrides only the transitions
/// it allows and returns the next state; everything else is rejected here, by default.
/// Adding a new status means adding a new class, not editing <c>if</c>s spread across <see cref="Order"/> (OCP).
/// States hold no data, so a single instance of each is shared.
/// </summary>
internal abstract class OrderState
{
    public static readonly OrderState Created = new CreatedState();
    public static readonly OrderState Confirmed = new ConfirmedState();
    public static readonly OrderState InPreparation = new InPreparationState();
    public static readonly OrderState Ready = new ReadyState();
    public static readonly OrderState Delivered = new DeliveredState();
    public static readonly OrderState Cancelled = new CancelledState();

    public abstract OrderStatus Status { get; }

    /// <summary>Whether lines can be added, changed or removed in this state.</summary>
    public virtual bool CanEditLines => false;

    public static OrderState From(OrderStatus status) => status switch
    {
        OrderStatus.Created => Created,
        OrderStatus.Confirmed => Confirmed,
        OrderStatus.InPreparation => InPreparation,
        OrderStatus.Ready => Ready,
        OrderStatus.Delivered => Delivered,
        OrderStatus.Cancelled => Cancelled,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown order status."),
    };

    public virtual OrderState Confirm() => throw InvalidTransition("confirm");

    public virtual OrderState StartPreparation() => throw InvalidTransition("start preparing");

    public virtual OrderState MarkReady() => throw InvalidTransition("mark as ready");

    public virtual OrderState Deliver() => throw InvalidTransition("deliver");

    public virtual OrderState Cancel() => throw InvalidTransition("cancel");

    private DomainException InvalidTransition(string action) =>
        new($"Cannot {action} an order that is {Status}.");
}
