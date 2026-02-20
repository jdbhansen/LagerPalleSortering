using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Tests;

public sealed class BarcodeScannerCompatibilityTests
{
    [Theory]
    [InlineData("]E0036000291452", "0036000291452")]
    [InlineData("]E073513537", "73513537")]
    [InlineData("]C1item-abc", "ITEM-ABC")]
    [InlineData(" ]Q3abc123 ", "ABC123")]
    public void ProductNormalizer_WithCommonScannerPrefixes_NormalizesExpectedValue(string rawScan, string expected)
    {
        var normalizer = new DefaultProductBarcodeNormalizer();

        var result = normalizer.Normalize(rawScan);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("PALLET:P-001", "P-001")]
    [InlineData("  pallet:p-001  ", "P-001")]
    [InlineData("]C1PALLET:P-001", "P-001")]
    [InlineData("]e0p+001", "P-001")]
    [InlineData("##PALLETÆP+0Æ0?1", "P-001")]
    public void PalletParser_WithScannerNoise_ParsesCanonicalPalletId(string scan, string expectedPalletId)
    {
        var service = new DefaultPalletBarcodeService();

        var success = service.TryParsePalletCode(scan, out var palletId);

        Assert.True(success);
        Assert.Equal(expectedPalletId, palletId);
    }

    [Theory]
    [InlineData("]C1PALLET:")]
    [InlineData("]E0ITEM-123")]
    [InlineData("PALLET:P-ABC")]
    [InlineData("PALLET:--")]
    public void PalletParser_WithInvalidPayload_ReturnsFalse(string scan)
    {
        var service = new DefaultPalletBarcodeService();

        var success = service.TryParsePalletCode(scan, out var palletId);

        Assert.False(success);
        Assert.Equal(string.Empty, palletId);
    }
}
