namespace Pos.Domain.Common;

/// <summary>Normalizes phone numbers to international digits (E.164 without the plus), as WhatsApp expects.</summary>
public static class PhoneNumber
{
    public const int MaxLength = 15;
    private const int MinLength = 8;

    /// <summary>
    /// Returns the number as international digits (e.g. <c>923001234567</c>), or <c>null</c> when it is not a usable number.
    /// Local numbers (a leading 0, or 10 digits or fewer) get <paramref name="defaultCountryCode"/>.
    /// </summary>
    public static string? Normalize(string? raw, string? defaultCountryCode)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var trimmed = raw.Trim();
        var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0)
            return null;

        var countryCode = new string((defaultCountryCode ?? "").Where(char.IsAsciiDigit).ToArray());

        if (trimmed.StartsWith('+'))
        {
            // Already international.
        }
        else if (digits.StartsWith("00"))
        {
            digits = digits[2..];
        }
        else if (countryCode.Length > 0 && digits.StartsWith('0'))
        {
            digits = countryCode + digits.TrimStart('0');
        }
        else if (countryCode.Length > 0 && digits.Length <= 10 && !digits.StartsWith(countryCode))
        {
            digits = countryCode + digits;
        }

        return digits.Length is >= MinLength and <= MaxLength && digits[0] != '0' ? digits : null;
    }
}
