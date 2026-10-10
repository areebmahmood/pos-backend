namespace Pos.Domain.Exceptions;

/// <summary>
/// Raised when a business invariant is violated (e.g. selling more than is in stock).
/// </summary>
public class DomainException(string message) : Exception(message);
