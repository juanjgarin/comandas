using Comandas.Orders.Domain.Discounts;
using Comandas.SharedKernel.Domain;
using Microsoft.Extensions.Options;

namespace Comandas.Orders.Application.Common;

/// <summary>
/// Validates the discount settings when the service starts (with <c>ValidateOnStart</c>), so a wrong
/// configuration stops the deployment instead of failing on a customer's bill. The rules are not repeated
/// here: building the factory runs the domain's own validation (DRY).
/// </summary>
internal sealed class DiscountOptionsValidator : IValidateOptions<DiscountOptions>
{
    public ValidateOptionsResult Validate(string? name, DiscountOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            _ = new DiscountFactory(TimeProvider.System, options.ToPolicy());
            return ValidateOptionsResult.Success;
        }
        catch (DomainException exception)
        {
            return ValidateOptionsResult.Fail($"Invalid '{DiscountOptions.SectionName}' configuration: {exception.Message}");
        }
    }
}
