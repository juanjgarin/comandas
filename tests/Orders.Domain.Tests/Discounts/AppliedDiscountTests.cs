using Comandas.Orders.Domain.Discounts;
using Comandas.SharedKernel.Domain;
using Comandas.SharedKernel.ValueObjects;

namespace Comandas.Orders.Domain.Tests.Discounts;

public sealed class AppliedDiscountTests
{
    [Fact]
    public void Of_ValidValues_TrimsDescription()
    {
        var discount = AppliedDiscount.Of("  10% off ", Money.Of(2m, "EUR"));

        discount.Description.Should().Be("10% off");
        discount.Amount.Should().Be(Money.Of(2m, "EUR"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Of_EmptyDescription_ThrowsDomainException(string description)
    {
        var act = () => AppliedDiscount.Of(description, Money.Of(2m, "EUR"));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Of_DescriptionTooLong_ThrowsDomainException()
    {
        var act = () => AppliedDiscount.Of(new string('x', AppliedDiscount.DescriptionMaxLength + 1), Money.Of(2m, "EUR"));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Of_ZeroAmount_ThrowsDomainException()
    {
        var act = () => AppliedDiscount.Of("Nothing", Money.Zero("EUR"));

        act.Should().Throw<DomainException>().WithMessage("*save something*");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        AppliedDiscount.Of("10% off", Money.Of(2m, "EUR"))
            .Should().Be(AppliedDiscount.Of("10% off", Money.Of(2m, "EUR")));
    }
}
