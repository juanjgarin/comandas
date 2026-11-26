using Comandas.Orders.Domain.Orders;
using Comandas.SharedKernel.Domain;

namespace Comandas.Orders.Domain.Tests.Orders;

public sealed class QuantityTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(99)]
    public void Of_WithValueWithinBounds_CreatesQuantity(int value)
    {
        var quantity = Quantity.Of(value);

        quantity.Value.Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Of_WithValueOutOfBounds_ThrowsDomainException(int value)
    {
        var act = () => Quantity.Of(value);

        act.Should().Throw<DomainException>().WithMessage("*between 1 and 99*");
    }

    [Fact]
    public void One_HasValueOne()
    {
        Quantity.One.Should().Be(Quantity.Of(1));
    }

    [Fact]
    public void Equals_WithSameValue_ReturnsTrue()
    {
        (Quantity.Of(3) == Quantity.Of(3)).Should().BeTrue();
        Quantity.Of(3).Should().NotBe(Quantity.Of(4));
    }

    [Fact]
    public void Add_WithinBounds_ReturnsSum()
    {
        var result = Quantity.Of(2) + Quantity.Of(3);

        result.Should().Be(Quantity.Of(5));
    }

    [Fact]
    public void Add_ExceedingMax_ThrowsDomainException()
    {
        var act = () => Quantity.Of(60) + Quantity.Of(40);

        act.Should().Throw<DomainException>();
    }
}
