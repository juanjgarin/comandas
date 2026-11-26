namespace Comandas.SharedKernel.Domain;

/// <summary>
/// Thrown when an operation would break a business rule (an invariant).
/// The API layer translates it into a ProblemDetails response (phase 2).
/// </summary>
public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
