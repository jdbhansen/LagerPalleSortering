namespace LagerPalleSortering.Domain;

public static class WarehouseBarcode
{
    public const string PalletPrefix = "PALLET:";
    private static readonly IPalletBarcodeService DefaultService = new DefaultPalletBarcodeService();

    public static string CreatePalletCode(string palletId) => DefaultService.CreatePalletCode(palletId);

    public static bool TryParsePalletCode(string scannedValue, out string palletId) =>
        DefaultService.TryParsePalletCode(scannedValue, out palletId);
}
