using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Tests;

public sealed class ProductBarcodeParserTests
{
    [Fact]
    public void Normalize_WithUpcA_ReturnsEquivalentEan13()
    {
        var result = ProductBarcodeParser.Normalize("036000291452");

        Assert.Equal("0036000291452", result);
    }

    [Fact]
    public void Normalize_WithScannerPrefix_ReturnsBarcodeWithoutPrefix()
    {
        var result = ProductBarcodeParser.Normalize("]E05701234567892");

        Assert.Equal("5701234567892", result);
    }

    [Fact]
    public void Normalize_WithAlphanumericValue_ReturnsUppercase()
    {
        var result = ProductBarcodeParser.Normalize("item-abc");

        Assert.Equal("ITEM-ABC", result);
    }
}
