namespace Pos.Application.Common.Abstractions.Services;

public interface IBarcodeGenerator
{
    /// <summary>Generates a barcode that is not yet used by any product.</summary>
    Task<string> GenerateUniqueAsync(CancellationToken ct);
}
