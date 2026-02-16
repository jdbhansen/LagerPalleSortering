namespace LagerPalleSortering.Domain;

public static class ProductBarcodeParser
{
    private static readonly IProductBarcodeNormalizer DefaultNormalizer = new DefaultProductBarcodeNormalizer();

    public static string Normalize(string rawValue) => DefaultNormalizer.Normalize(rawValue);
}
