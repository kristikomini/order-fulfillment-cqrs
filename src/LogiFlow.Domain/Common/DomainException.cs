namespace LogiFlow.Domain.Common;

/// <summary>
/// Thrown when an operation would violate a domain invariant (e.g. confirming
/// an order that is not pending). Surfaced by the API as a 409/422, never a 500.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
