using Pos.Domain.Exceptions;

namespace Pos.Domain.Entities;

/// <summary>An image the store prints on receipts, keyed by what it is for.</summary>
public class StoreImage
{
    public const string WhatsAppGroupQr = "whatsapp-group-qr";
    public const int KindMaxLength = 50;

    private StoreImage() { }

    public StoreImage(string kind, byte[] content, DateTime utcNow)
    {
        Kind = kind;
        Replace(content, utcNow);
    }

    public string Kind { get; private set; } = default!;
    public byte[] Content { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public DateTime UpdatedAt { get; private set; }

    public void Replace(byte[] content, DateTime utcNow)
    {
        if (content.Length == 0)
            throw new DomainException("The image is empty.");
        if (content.Length > ProductImage.MaxBytes)
            throw new DomainException($"The image must be at most {ProductImage.MaxBytes / 1024} KB.");

        ContentType = ProductImage.DetectContentType(content)
            ?? throw new DomainException("The image must be a PNG, JPEG, WebP or GIF file.");
        Content = content;
        UpdatedAt = utcNow;
    }
}
