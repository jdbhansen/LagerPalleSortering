using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace LagerPalleSortering.Services;

public sealed class BarcodeService
{
    public string GenerateCode128Svg(string content, int width = 520, int height = 120)
    {
        var value = (content ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var writer = new BarcodeWriterSvg
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 4,
                PureBarcode = true
            }
        };

        return writer.Write(value).Content;
    }
}
