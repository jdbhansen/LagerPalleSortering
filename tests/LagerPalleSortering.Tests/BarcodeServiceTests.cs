using LagerPalleSortering.Services;

namespace LagerPalleSortering.Tests;

public sealed class BarcodeServiceTests
{
    [Fact]
    public void GenerateCode128Svg_WithValidInput_ReturnsSvgContent()
    {
        var service = new BarcodeService();

        var svg = service.GenerateCode128Svg("5701234567892");

        Assert.False(string.IsNullOrWhiteSpace(svg));
        Assert.Contains("<svg", svg, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateCode128Svg_WithEmptyInput_ReturnsEmptyString()
    {
        var service = new BarcodeService();

        var svg = service.GenerateCode128Svg("   ");

        Assert.Equal(string.Empty, svg);
    }
}
