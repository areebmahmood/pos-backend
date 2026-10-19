namespace Pos.Domain.Common;

/// <summary>Normalizes phone numbers to international digits (E.164 without the plus), as WhatsApp expects.</summary>
public static class PhoneNumber
{
    public const int MaxLength = 15;
    private const int MinLength = 8;

    /// <summary>Digits in a local Pakistani mobile number, e.g. 03001234567.</summary>
    public const int LocalMobileLength = 11;

    /// <summary>Example shown in validation messages.</summary>
    public const string MobileFormatHint = "11 digits starting with a Pakistani network code (030x Jazz, 031x Zong, 032x Jazz/Warid, 033x Ufone, 034x Telenor or 0355 SCO), e.g. 03001234567, or +92 followed by 10 digits";

    /// <summary>
    /// Mobile network codes in use in Pakistan, as the digits after the leading 0 (or after 92):
    /// 30x Jazz, 31x Zong, 32x Jazz (ex-Warid), 33x Ufone/Onic, 34x Telenor, 355 SCO.
    /// Whole operator blocks are allowed so new codes inside a block (and ported numbers) keep working.
    /// </summary>
    private static readonly string[] NetworkCodePrefixes = ["30", "31", "32", "33", "34", "355"];

    /// <summary>
    /// True for a Pakistani mobile number: <c>03</c> plus 9 digits (11 in all), or <c>+92</c> / <c>0092</c> / <c>92</c>
    /// plus <c>3</c> and 9 digits, whose network code belongs to a Pakistani mobile operator (see
    /// <see cref="NetworkCodePrefixes"/>). Spaces and dashes are ignored.
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

        var national = compact.Length switch
        {
            LocalMobileLength when compact.StartsWith('0') => compact[1..],
            12 when compact.StartsWith("92") => compact[2..],
            _ => null
        };

        return national is not null && NetworkCodePrefixes.Any(national.StartsWith);
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
