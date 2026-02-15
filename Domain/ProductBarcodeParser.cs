namespace LagerPalleSortering.Domain;

public static class ProductBarcodeParser
{
    public static string Normalize(string rawValue)
    {
        var value = StripScannerPrefix((rawValue ?? string.Empty).Trim());
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (IsDigitsOnly(value))
        {
            if (value.Length == 12 && IsValidCheckDigit(value))
            {
                // Normalize UPC-A to EAN-13 so equivalent scans group consistently.
                return $"0{value}";
            }

            if (value.Length == 8 && IsValidCheckDigit(value))
            {
                return value;
            }

            if (value.Length == 13 && IsValidCheckDigit(value))
            {
                return value;
            }
        }

        return value.ToUpperInvariant();
    }

    private static string StripScannerPrefix(string value)
    {
        // Many scanners prepend a symbology identifier, e.g. ]E0 for EAN/UPC.
        if (value.Length >= 3 && value[0] == ']')
        {
            return value[3..];
        }

        return value;
    }

    private static bool IsDigitsOnly(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidCheckDigit(string code)
    {
        // Generic EAN/UPC check-digit validation.
        var sum = 0;
        var isEvenFromRight = true;

        for (var i = code.Length - 2; i >= 0; i--)
        {
            var digit = code[i] - '0';
            sum += isEvenFromRight ? digit * 3 : digit;
            isEvenFromRight = !isEvenFromRight;
        }

        var check = (10 - (sum % 10)) % 10;
        return check == code[^1] - '0';
    }
}
