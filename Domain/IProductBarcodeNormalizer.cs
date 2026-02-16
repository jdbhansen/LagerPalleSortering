namespace LagerPalleSortering.Domain;

public interface IProductBarcodeNormalizer
{
    string Normalize(string rawValue);
}
