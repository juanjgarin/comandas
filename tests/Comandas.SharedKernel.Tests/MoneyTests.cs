using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.SharedKernel.Tests;

public sealed class MoneyTests
{
    [Fact]
    public void Of_WithValidAmountAndCurrency_CreatesMoney()
    {
        var money = Money.Of(12.50m, "EUR");

        money.Amount.Should().Be(12.50m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Of_WithLowerCaseCurrency_NormalizesToUpperCase()
    {
        var money = Money.Of(1m, " eur ");

        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Of_WithNegativeAmount_ThrowsDomainException()
    {
        var act = () => Money.Of(-0.01m, "EUR");

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData(null)]
    public void Of_WithInvalidCurrency_ThrowsDomainException(string? currency)
    {
        var act = () => Money.Of(1m, currency!);

        act.Should().Throw<DomainException>().WithMessage("*ISO 4217*");
    }

    [Theory]
    [InlineData(2.345, 2.35)] // half rounds away from zero (banker's rounding would give 2.34)
    [InlineData(2.344, 2.34)]
    [InlineData(2.3, 2.30)]
    public void Of_WithMoreThanTwoDecimals_RoundsHalfAwayFromZero(decimal amount, decimal expected)
    {
        var money = Money.Of(amount, "EUR");

        money.Amount.Should().Be(expected);
    }

    [Fact]
    public void Equals_WithSameAmountAndCurrency_ReturnsTrue()
    {
        var first = Money.Of(10m, "EUR");
        var second = Money.Of(10.00m, "eur");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Theory]
    [InlineData(10, "USD")]
    [InlineData(11, "EUR")]
    public void Equals_WithDifferentAmountOrCurrency_ReturnsFalse(decimal amount, string currency)
    {
        var euros = Money.Of(10m, "EUR");
        var other = Money.Of(amount, currency);

        euros.Should().NotBe(other);
        (euros != other).Should().BeTrue();
    }

    [Fact]
    public void Add_WithSameCurrency_ReturnsNewMoneyWithSum()
    {
        var first = Money.Of(10.25m, "EUR");
        var second = Money.Of(4.75m, "EUR");

        var result = first + second;

        result.Should().Be(Money.Of(15m, "EUR"));
        first.Amount.Should().Be(10.25m, "Money is immutable: operations never change the operands");
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ThrowsDomainException()
    {
        var act = () => Money.Of(1m, "EUR") + Money.Of(1m, "USD");

        act.Should().Throw<DomainException>().WithMessage("*EUR*USD*");
    }

    [Fact]
    public void Subtract_WithSmallerAmount_ReturnsDifference()
    {
        var result = Money.Of(10m, "EUR") - Money.Of(2.50m, "EUR");

        result.Should().Be(Money.Of(7.50m, "EUR"));
    }

    [Fact]
    public void Subtract_ToExactlyZero_ReturnsZero()
    {
        var result = Money.Of(5m, "EUR") - Money.Of(5m, "EUR");

        result.Should().Be(Money.Zero("EUR"));
    }

    [Fact]
    public void Subtract_WithLargerAmount_ThrowsDomainException()
    {
        var act = () => Money.Of(1m, "EUR") - Money.Of(2m, "EUR");

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Subtract_WithDifferentCurrencies_ThrowsDomainException()
    {
        var act = () => Money.Of(5m, "EUR") - Money.Of(1m, "USD");

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(3, 7.50)]     // a quantity
    [InlineData(0.15, 0.38)]  // a 15% discount: 0.375 rounds to 0.38
    [InlineData(0, 0)]
    public void Multiply_ByNonNegativeFactor_ReturnsRoundedProduct(decimal factor, decimal expected)
    {
        var result = Money.Of(2.50m, "EUR") * factor;

        result.Should().Be(Money.Of(expected, "EUR"));
    }

    [Fact]
    public void Multiply_ByNegativeFactor_ThrowsDomainException()
    {
        var act = () => Money.Of(2.50m, "EUR") * -1m;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ToString_FormatsWithTwoDecimalsAndCurrency()
    {
        Money.Of(7.5m, "EUR").ToString().Should().Be("7.50 EUR");
    }
}
