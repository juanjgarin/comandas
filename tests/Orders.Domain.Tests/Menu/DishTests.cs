using Comandas.Orders.Domain.Menu;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Tests.Menu;

public sealed class DishTests
{
    private static readonly Money TenEuros = Money.Of(10m, "EUR");

    [Fact]
    public void Create_WithValidData_CreatesAvailableDish()
    {
        var dish = Dish.Create("  Burger  ", TenEuros);

        dish.Id.Should().NotBeEmpty();
        dish.Name.Should().Be("Burger", "names are trimmed");
        dish.BasePrice.Should().Be(TenEuros);
        dish.IsAvailable.Should().BeTrue();
        dish.IsTwoForOneEligible.Should().BeFalse();
    }

    [Fact]
    public void Create_TwoDishes_GeneratesDifferentIds()
    {
        var first = Dish.Create("Burger", TenEuros);
        var second = Dish.Create("Burger", TenEuros);

        first.Id.Should().NotBe(second.Id);
        first.Should().NotBe(second, "same data, different identity: they are different entities");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData(null)]
    public void Create_WithInvalidName_ThrowsDomainException(string? name)
    {
        var act = () => Dish.Create(name!, TenEuros);

        act.Should().Throw<DomainException>().WithMessage("*between 2 and 100*");
    }

    [Fact]
    public void Create_WithTooLongName_ThrowsDomainException()
    {
        var act = () => Dish.Create(new string('x', MenuItem.NameMaxLength + 1), TenEuros);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_WithZeroPrice_ThrowsDomainException()
    {
        var act = () => Dish.Create("Burger", Money.Zero("EUR"));

        act.Should().Throw<DomainException>().WithMessage("*greater than zero*");
    }

    [Fact]
    public void GetPrice_CalledThroughBaseType_ReturnsBasePrice()
    {
        // The caller only knows it has "some MenuItem": the subclass decides how the price is calculated.
        MenuItem item = Dish.Create("Burger", TenEuros);

        item.GetPrice().Should().Be(TenEuros);
    }

    [Fact]
    public void ChangePrice_WithValidPrice_UpdatesPrice()
    {
        var dish = Dish.Create("Burger", TenEuros);

        dish.ChangePrice(Money.Of(12.50m, "EUR"));

        dish.GetPrice().Should().Be(Money.Of(12.50m, "EUR"));
    }

    [Fact]
    public void ChangePrice_ToZero_ThrowsDomainExceptionAndKeepsPreviousPrice()
    {
        var dish = Dish.Create("Burger", TenEuros);

        var act = () => dish.ChangePrice(Money.Zero("EUR"));

        act.Should().Throw<DomainException>();
        dish.BasePrice.Should().Be(TenEuros, "a failed operation must leave the entity unchanged");
    }

    [Fact]
    public void Rename_WithValidName_UpdatesName()
    {
        var dish = Dish.Create("Burger", TenEuros);

        dish.Rename("Cheeseburger");

        dish.Name.Should().Be("Cheeseburger");
    }

    [Fact]
    public void MarkUnavailable_ThenMarkAvailable_TogglesAvailability()
    {
        var dish = Dish.Create("Burger", TenEuros);

        dish.MarkUnavailable();
        dish.IsAvailable.Should().BeFalse();

        dish.MarkAvailable();
        dish.IsAvailable.Should().BeTrue();
    }

    [Fact]
    public void EnableTwoForOne_ThenDisable_TogglesFlag()
    {
        var dish = Dish.Create("Burger", TenEuros);

        dish.EnableTwoForOne();
        dish.IsTwoForOneEligible.Should().BeTrue();

        dish.DisableTwoForOne();
        dish.IsTwoForOneEligible.Should().BeFalse();
    }
}
