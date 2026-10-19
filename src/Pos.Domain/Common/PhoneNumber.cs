namespace Pos.Domain.Common;

/// <summary>Normalizes phone numbers to international digits (E.164 without the plus), as WhatsApp expects.</summary>
public static class PhoneNumber
{
    public const int MaxLength = 15;
    private const int MinLength = 8;

    /// <summary>Digits in a local Pakistani mobile number, e.g. 03001234567.</summary>
    public const int LocalMobileLength = 11;

    /// <summary>Example shown in validation messages.</summary>
    public const string MobileFormatHint = "11 digits starting with 03, e.g. 03001234567, or +92 followed by 10 digits";

    /// <summary>
    /// True for a Pakistani mobile number: <c>03</c> plus 9 digits (11 in all), or <c>+92</c> / <c>0092</c> / <c>92</c>
    /// plus <c>3</c> and 9 digits. Spaces and dashes are ignored.
    /// </summary>
    public static bool IsValidMobile(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var compact = string.Concat(raw.Where(c => c is not (' ' or '-')));
        if (compact.StartsWith('+'))
            compact = compact[1..];
        else if (compact.StartsWith("00"))
            compact = compact[2..];

        if (!compact.All(char.IsAsciiDigit))
            return false;

        return compact.Length switch
        {
            LocalMobileLength => compact.StartsWith("03"),
            12 => compact.StartsWith("923"),
            _ => false
        };
    }

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
