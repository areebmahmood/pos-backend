using System.Security.Cryptography;
using Pos.Application.Common.Abstractions.Persistence;
using Pos.Application.Common.Abstractions.Services;

namespace Pos.Infrastructure.Services;

/// <summary>
/// Generates EAN-13 barcodes in the 20–29 "in-store use" prefix range so they never clash with manufacturer codes.
/// </summary>
internal sealed class Ean13BarcodeGenerator(IProductRepository products) : IBarcodeGenerator
{
    private const int MaxAttempts = 10;

    public async Task<string> GenerateUniqueAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var barcode = Generate();
            if (!await products.BarcodeExistsAsync(barcode, null, ct))
                return barcode;
        }

        throw new InvalidOperationException("Could not generate a unique barcode.");
    }

    internal static string Generate()
    {
        Span<int> digits = stackalloc int[12];
        digits[0] = 2;
        digits[1] = 0;
        for (var i = 2; i < digits.Length; i++)
            digits[i] = RandomNumberGenerator.GetInt32(10);

        var payload = string.Concat(digits.ToArray());
        return payload + CheckDigit(payload);
    }

    internal static int CheckDigit(string twelveDigits)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (twelveDigits[i] - '0') * (i % 2 == 0 ? 1 : 3);

        return (10 - sum % 10) % 10;
    }
}
