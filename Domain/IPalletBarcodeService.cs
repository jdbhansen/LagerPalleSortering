namespace LagerPalleSortering.Domain;

public interface IPalletBarcodeService
{
    string CreatePalletCode(string palletId);
    bool TryParsePalletCode(string scannedValue, out string palletId);
}
