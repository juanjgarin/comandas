namespace Comandas.Orders.Application.Orders.Commands.StartOrderPreparation;

/// <summary>The kitchen starts cooking the order (Confirmed -> InPreparation).</summary>
public sealed record StartOrderPreparationCommand(Guid OrderId) : IOrderCommand;
