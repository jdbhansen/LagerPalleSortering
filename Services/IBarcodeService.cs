namespace LagerPalleSortering.Services;

public interface IBarcodeService
{
    string GenerateCode128Svg(string content, int width = 520, int height = 120);
}
