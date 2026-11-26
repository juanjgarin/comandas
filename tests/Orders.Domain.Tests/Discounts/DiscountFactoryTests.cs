using System.Globalization;
using Comandas.Orders.Domain.Discounts;
using Comandas.Orders.Domain.Menu;
using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;
using Microsoft.Extensions.Time.Testing;

namespace Comandas.Orders.Domain.Tests.Discounts;

/// <summary>Selection rules of the Factory (decision D6: the best single discount wins).</summary>
public sealed class DiscountFactoryTests
{
    // Happy hour: 20% from 18:00 to 20:00.
    private static readonly DiscountPolicy Policy = new(new TimeOnly(18, 0), new TimeOnly(20, 0), 20m);

    [Fact]
    public void CreateBestFor_HappyHourBeatsTwoForOne_WhenItSavesMore()
    {
        // 27.50 at 19:00 with 3 eligible sodas: happy hour 5.50 vs 2-for-1 2.50.
        var factory = FactoryAt("19:00");

        var discount = factory.CreateBestFor(Context(burgers: 2, eligibleSodas: 3), DiscountRequest.None);

        discount.Should().BeOfType<HappyHourDiscount>();
        discount.CalculateDiscount(Context(burgers: 2, eligibleSodas: 3)).Should().Be(Money.Of(5.50m, "EUR"));
    }

    [Fact]
    public void CreateBestFor_TwoForOneWins_WhenItSavesMoreThanHappyHour()
    {
        // 10 eligible sodas = 25.00: 2-for-1 gives 5 free (12.50) vs happy hour 20% (5.00).
        var discount = FactoryAt("19:00").CreateBestFor(Context(burgers: 0, eligibleSodas: 10), DiscountRequest.None);

        discount.Should().BeOfType<TwoForOneDiscount>();
    }

    [Fact]
    public void CreateBestFor_OutsideHappyHour_WithEligibleItems_ReturnsTwoForOne()
    {
        var discount = FactoryAt("13:00").CreateBestFor(Context(burgers: 2, eligibleSodas: 3), DiscountRequest.None);

        discount.Should().BeOfType<TwoForOneDiscount>();
    }

    [Fact]
    public void CreateBestFor_NothingApplies_ReturnsNoDiscountNullObject()
    {
        var context = Context(burgers: 2, eligibleSodas: 1); // one soda: no pair
        var discount = FactoryAt("13:00").CreateBestFor(context, DiscountRequest.None);

        discount.Should().BeSameAs(NoDiscount.Instance);
        discount.CalculateDiscount(context).Should().Be(Money.Zero("EUR"), "a Null Object is safe to use");
    }

    [Fact]
    public void CreateBestFor_ManualPercentageHigherThanAutomatic_ReturnsManual()
    {
        var discount = FactoryAt("19:00").CreateBestFor(
            Context(burgers: 2, eligibleSodas: 3), new DiscountRequest(ManualPercentage: 50m));

        discount.Should().BeOfType<PercentageDiscount>().Which.Percentage.Should().Be(50m);
    }

    [Fact]
    public void CreateBestFor_ManualPercentageLowerThanAutomatic_BestStillWins()
    {
        // Staff 10% (2.75) during happy hour 20% (5.50): not combinable, the customer gets the better one.
        var discount = FactoryAt("19:00").CreateBestFor(
            Context(burgers: 2, eligibleSodas: 3), new DiscountRequest(ManualPercentage: 10m));

        discount.Should().BeOfType<HappyHourDiscount>();
    }

    [Fact]
    public void CreateBestFor_Tie_ManualPercentageWins()
    {
        // Manual 20% vs happy hour 20%: same amount; the explicit request is the one shown on the bill.
        var discount = FactoryAt("19:00").CreateBestFor(
            Context(burgers: 2, eligibleSodas: 3), new DiscountRequest(ManualPercentage: 20m));

        discount.Should().BeOfType<PercentageDiscount>();
    }

    [Fact]
    public void CreateBestFor_InvalidManualPercentage_ThrowsDomainException()
    {
        var act = () => FactoryAt("19:00").CreateBestFor(
            Context(burgers: 1, eligibleSodas: 0), new DiscountRequest(ManualPercentage: 120m));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Constructor_InvalidPolicy_FailsImmediately()
    {
        var badPolicy = new DiscountPolicy(new TimeOnly(18, 0), new TimeOnly(18, 0), 20m);

        var act = () => new DiscountFactory(TimeProvider.System, badPolicy);

        act.Should().Throw<DomainException>("a wrong configuration must fail at startup, not on a customer's bill");
    }

    private static DiscountFactory FactoryAt(string localTime)
    {
        var time = TimeOnly.Parse(localTime, CultureInfo.InvariantCulture);
        var clock = new FakeTimeProvider(new DateTimeOffset(new DateOnly(2026, 11, 26), time, TimeSpan.Zero));
        clock.SetLocalTimeZone(TimeZoneInfo.Utc);
        return new DiscountFactory(clock, Policy);
    }

    /// <summary>Burgers at 10.00 (not eligible) and sodas at 2.50 (2-for-1 eligible).</summary>
    private static DiscountContext Context(int burgers, int eligibleSodas)
    {
        var order = Order.Create(1, "EUR");

        if (burgers > 0)
        {
            order.AddItem(Dish.Create("Burger", Money.Of(10m, "EUR")), Quantity.Of(burgers));
        }

        if (eligibleSodas > 0)
        {
            var soda = Dish.Create("Soda", Money.Of(2.50m, "EUR"));
            soda.EnableTwoForOne();
            order.AddItem(soda, Quantity.Of(eligibleSodas));
        }

        return DiscountContext.For(order);
    }
}
