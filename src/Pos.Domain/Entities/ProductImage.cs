using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

/// <summary>
/// Optional image or logo printed on a product's barcode label in place of the store and product name.
/// Kept in its own table so loading a product never loads the image bytes.
/// </summary>
public class ProductImage
{
    public const int MaxBytes = 1024 * 1024;
    public const int ContentTypeMaxLength = 50;

    private ProductImage() { }

    public ProductImage(int productId, byte[] content, DateTime utcNow)
    {
        ProductId = productId;
        Replace(content, utcNow);
    }

    public int ProductId { get; private set; }
    public byte[] Content { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public DateTime UpdatedAt { get; private set; }

    public void Replace(byte[] content, DateTime utcNow)
    {
        if (content.Length == 0)
            throw new DomainException("The image is empty.");
        if (content.Length > MaxBytes)
            throw new DomainException($"The image must be at most {MaxBytes / 1024} KB.");

        ContentType = DetectContentType(content)
            ?? throw new DomainException("The image must be a PNG, JPEG, WebP or GIF file.");
        Content = content;
        UpdatedAt = utcNow;
    }

    /// <summary>Identifies the supported formats by their file signature, ignoring any client-supplied type.</summary>
    public static string? DetectContentType(ReadOnlySpan<byte> data)
    {
        if (data.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]))
            return "image/png";
        if (data.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]))
            return "image/jpeg";
        if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8))
            return "image/gif";
        if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8))
            return "image/webp";
        return null;
    }
}
