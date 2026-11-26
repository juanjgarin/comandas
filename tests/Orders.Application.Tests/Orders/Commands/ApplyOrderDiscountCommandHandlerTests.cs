using Comandas.Orders.Application.Orders.Commands.ApplyOrderDiscount;
using Comandas.Orders.Application.Tests.Fakes;
using Comandas.Orders.Domain.Discounts;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace Comandas.Orders.Application.Tests.Orders.Commands;

/// <summary>Uses the REAL DiscountFactory with a fake clock: the handler's job is wiring factory and aggregate.</summary>
public sealed class ApplyOrderDiscountCommandHandlerTests
{
    // Happy hour: 20% from 18:00 to 20:00.
    private static readonly DiscountPolicy Policy = new(new TimeOnly(18, 0), new TimeOnly(20, 0), 20m);

    private readonly InMemoryOrderRepository _orders = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _clock = new();

    public ApplyOrderDiscountCommandHandlerTests()
    {
        _clock.SetLocalTimeZone(TimeZoneInfo.Utc);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_DuringHappyHour_AppliesHappyHourAndSaves()
    {
        SetTime(19, 0);
        var order = ExistingOrderOf20Euros();

        await Handler().Handle(new ApplyOrderDiscountCommand(order.Id), Ct);

        order.Discount!.Amount.Should().Be(Money.Of(4m, "EUR")); // Applied just above.
        order.Total.Should().Be(Money.Of(16m, "EUR"));
        _unitOfWork.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ManualPercentageBetterThanAutomatic_AppliesManual()
    {
        SetTime(19, 0);
        var order = ExistingOrderOf20Euros();

        await Handler().Handle(new ApplyOrderDiscountCommand(order.Id, ManualPercentage: 50m), Ct);

        order.Discount!.Description.Should().Be("50% off"); // Applied just above.
        order.Total.Should().Be(Money.Of(10m, "EUR"));
    }

    [Fact]
    public async Task Handle_NothingApplies_LeavesOrderWithoutDiscount()
    {
        SetTime(13, 0);
        var order = ExistingOrderOf20Euros();

        await Handler().Handle(new ApplyOrderDiscountCommand(order.Id), Ct);

        order.Discount.Should().BeNull();
        order.Total.Should().Be(Money.Of(20m, "EUR"));
    }

    [Fact]
    public async Task Handle_ConfirmedOrder_PropagatesDomainExceptionAndDoesNotSave()
    {
        SetTime(19, 0);
        var order = ExistingOrderOf20Euros();
        order.Confirm();

        var act = () => Handler().Handle(new ApplyOrderDiscountCommand(order.Id), Ct);

        await act.Should().ThrowAsync<DomainException>();
        _unitOfWork.SaveChangesCalls.Should().Be(0);
    }

    private ApplyOrderDiscountCommandHandler Handler() =>
        new(_orders, _unitOfWork, new DiscountFactory(_clock, Policy));

    private void SetTime(int hour, int minute) =>
        _clock.SetUtcNow(new DateTimeOffset(2026, 11, 26, hour, minute, 0, TimeSpan.Zero));

    private Order ExistingOrderOf20Euros()
    {
        var order = Order.Create(1, "EUR");
        order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.Of(2));
        _orders.Add(order);
        return order;
    }
}
