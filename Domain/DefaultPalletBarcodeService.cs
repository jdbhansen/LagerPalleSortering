namespace LagerPalleSortering.Domain;

/// <summary>
/// Parses pallet scans with tolerance for common scanner noise.
/// </summary>
public sealed class DefaultPalletBarcodeService : IPalletBarcodeService
{
    private const string PalletPrefix = "PALLET:";
    private const string PalletIdPrefix = "P-";
    private const string LegacySeparator = "+";
    private const string CanonicalSeparator = "-";

    public string CreatePalletCode(string palletId)
    {
        var id = NormalizePalletId(palletId);
        return $"{PalletPrefix}{id}";
    }

    public bool TryParsePalletCode(string scannedValue, out string palletId)
    {
        palletId = string.Empty;
        var value = NormalizeScannedPalletCode(scannedValue);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parsedPalletId = TryExtractPalletId(value);
        if (string.IsNullOrWhiteSpace(parsedPalletId))
        {
            return false;
        }

        palletId = parsedPalletId;
        return true;
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

    private static string TryExtractPalletId(string value)
    {
        // Keep compatibility with canonical barcode payloads (`PALLET:P-001`)
        // while still accepting raw/noisy scanner keyboard output.
        var searchValue = value.StartsWith(PalletPrefix, StringComparison.Ordinal)
            ? value[PalletPrefix.Length..]
            : value;

        var palletPrefixIndex = searchValue.IndexOf(PalletIdPrefix, StringComparison.Ordinal);
        if (palletPrefixIndex < 0)
        {
            return string.Empty;
        }

        var candidate = searchValue[(palletPrefixIndex + PalletIdPrefix.Length)..];
        var digitsOnly = ExtractDigits(candidate);
        if (digitsOnly.Length == 0)
        {
            return string.Empty;
        }

        // Canonical pallet id shape used across storage and labels.
        return $"P-{digitsOnly}";
    }

    private static string ExtractDigits(string value)
    {
        var chars = new char[value.Length];
        var index = 0;
        foreach (var ch in value)
        {
            if (char.IsAsciiDigit(ch))
            {
                chars[index++] = ch;
            }
        }

        return new string(chars, 0, index);
    }
}
