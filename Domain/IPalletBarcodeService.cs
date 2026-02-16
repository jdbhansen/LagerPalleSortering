namespace LagerPalleSortering.Domain;

/// <summary>
/// Creates and parses pallet barcode payloads.
/// </summary>
public interface IPalletBarcodeService
{
    string CreatePalletCode(string palletId);
    bool TryParsePalletCode(string scannedValue, out string palletId);
}
