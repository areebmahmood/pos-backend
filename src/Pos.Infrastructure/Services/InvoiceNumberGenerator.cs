using System.Security.Cryptography;
using Pos.Application.Common.Abstractions.Services;

namespace Pos.Infrastructure.Services;

/// <summary>
/// Produces invoice numbers like <c>INV-261010-7KQ2MX</c>: the store-local date plus 6 random Crockford base-32
/// characters. Numbers are unique without a database round-trip; the unique index is the final safeguard.
/// </summary>
internal sealed class InvoiceNumberGenerator(IStoreClock storeClock) : IInvoiceNumberGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public string Next()
    {
        Span<char> suffix = stackalloc char[6];
        for (var i = 0; i < suffix.Length; i++)
            suffix[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return $"INV-{storeClock.Today:yyMMdd}-{suffix}";
    }
}
