namespace LagerPalleSortering.Domain;

public static class WarehouseBarcode
{
    public const string PalletPrefix = "PALLET:";

    public static string CreatePalletCode(string palletId)
    {
        var id = NormalizePalletId(palletId);
        return $"{PalletPrefix}{id}";
    }

    public static bool TryParsePalletCode(string scannedValue, out string palletId)
    {
        palletId = string.Empty;
        var value = (scannedValue ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (value.StartsWith(PalletPrefix, StringComparison.Ordinal))
        {
            var parsed = value[PalletPrefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(parsed))
            {
                return false;
            }

            palletId = NormalizePalletId(parsed);
            return true;
        }

        // Backward compatibility with old labels without prefix.
        if (value.StartsWith("P-", StringComparison.Ordinal))
        {
            palletId = NormalizePalletId(value);
            return true;
        }

        return false;
    }

    private static string NormalizePalletId(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();
}
