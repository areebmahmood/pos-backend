using Pos.Domain.Exceptions;

namespace Pos.Domain.Common;

internal static class Guard
{
    public static string NotEmpty(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{name} must be at most {maxLength} characters.");

        return trimmed;
    }

    public static string? Optional(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{name} must be at most {maxLength} characters.");

        return trimmed;
    }

    public static decimal NotNegative(decimal value, string name)
    {
        if (value < 0)
            throw new DomainException($"{name} cannot be negative.");
        return value;
    }

    public static decimal Positive(decimal value, string name)
    {
        if (value <= 0)
            throw new DomainException($"{name} must be greater than zero.");
        return value;
    }
}
