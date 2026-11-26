using Comandas.Orders.Application.Common;

namespace Comandas.Orders.Application.Tests.Common;

public sealed class DiscountOptionsValidatorTests
{
    private readonly DiscountOptionsValidator _validator = new();

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        var result = _validator.Validate(name: null, new DiscountOptions());

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_PercentageOutOfRange_FailsWithDomainMessage()
    {
        var result = _validator.Validate(name: null, new DiscountOptions { HappyHourPercentage = 150m });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().StartWith("Invalid 'Discounts' configuration:");
    }

    [Fact]
    public void Validate_EmptyWindow_Fails()
    {
        var sameTime = new TimeOnly(18, 0);

        var result = _validator.Validate(name: null, new DiscountOptions { HappyHourStart = sameTime, HappyHourEnd = sameTime });

        result.Failed.Should().BeTrue();
    }
}
