using FluentValidation;
using ValidationException = Pos.Application.Common.Exceptions.ValidationException;

namespace Pos.Application.Common.Validation;

public static class ValidatorExtensions
{
    /// <summary>Validates <paramref name="instance"/> and throws an application <see cref="ValidationException"/> on failure.</summary>
    public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (result.IsValid)
            return;

        var errors = result.Errors
            .GroupBy(e => ToCamelCase(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

        throw new ValidationException(errors);
    }

    private static string ToCamelCase(string propertyName) =>
        string.Join('.', propertyName.Split('.').Select(part =>
            string.IsNullOrEmpty(part) ? part : char.ToLowerInvariant(part[0]) + part[1..]));
}
