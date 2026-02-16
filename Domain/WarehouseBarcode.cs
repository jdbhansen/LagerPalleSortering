namespace LagerPalleSortering.Domain;

public static class WarehouseBarcode
{
    public const string PalletPrefix = "PALLET:";
    private const string LegacySeparator = "+";
    private const string CanonicalSeparator = "-";

    public static string CreatePalletCode(string palletId)
    {
        var id = NormalizePalletId(palletId);
        return $"{PalletPrefix}{id}";
    }

    public static bool TryParsePalletCode(string scannedValue, out string palletId)
    {
        palletId = string.Empty;
        var value = NormalizeScannedPalletCode(scannedValue);
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

    private static string NormalizePalletId(string value) =>
        KeepAllowedPalletIdCharacters(
            (value ?? string.Empty)
                .Trim()
                .ToUpperInvariant()
                .Replace(LegacySeparator, CanonicalSeparator, StringComparison.Ordinal));

    private static string NormalizeScannedPalletCode(string value)
    {
        var normalized = (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace(LegacySeparator, CanonicalSeparator, StringComparison.Ordinal);

        return KeepAllowedPalletCodeCharacters(normalized);
    }

    private static string KeepAllowedPalletCodeCharacters(string value)
    {
        var chars = new char[value.Length];
        var index = 0;
        foreach (var ch in value)
        {
            if (ch is ':' or '-' || char.IsAsciiLetterOrDigit(ch))
            {
                chars[index++] = ch;
            }
        }

        return new string(chars, 0, index);
    }

    private static string KeepAllowedPalletIdCharacters(string value)
    {
        var chars = new char[value.Length];
        var index = 0;
        foreach (var ch in value)
        {
            if (ch == '-' || char.IsAsciiLetterOrDigit(ch))
            {
                chars[index++] = ch;
            }
        }

        return new string(chars, 0, index);
    }
}
