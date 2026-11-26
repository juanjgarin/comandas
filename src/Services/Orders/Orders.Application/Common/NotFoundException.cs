namespace Comandas.Orders.Application.Common;

/// <summary>
/// The requested resource does not exist. Not a business-rule violation (that is DomainException):
/// the API maps it to HTTP 404 (phase 2).
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public static NotFoundException For(string resource, Guid id) => new($"{resource} '{id}' was not found.");
}
