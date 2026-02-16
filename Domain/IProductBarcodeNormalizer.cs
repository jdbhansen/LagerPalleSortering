namespace LagerPalleSortering.Domain;

/// <summary>
/// Normalizes raw product barcode input to one canonical storage format.
/// </summary>
public interface IProductBarcodeNormalizer
{
    string Normalize(string rawValue);
}
