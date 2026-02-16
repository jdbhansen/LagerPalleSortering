namespace LagerPalleSortering.Domain;

public static class ProductBarcodeParser
{
    // Backward-compatible static wrapper used by older call-sites and tests.
    private static readonly IProductBarcodeNormalizer DefaultNormalizer = new DefaultProductBarcodeNormalizer();

    public static string Normalize(string rawValue) => DefaultNormalizer.Normalize(rawValue);
}
