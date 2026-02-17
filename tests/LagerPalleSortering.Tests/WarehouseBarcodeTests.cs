using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Tests;

public sealed class WarehouseBarcodeTests
{
    [Theory]
    [InlineData("PALLET:P-001", "P-001")]
    [InlineData("PALLETæP-001", "P-001")]
    [InlineData("PALLETÆP-001", "P-001")]
    [InlineData("PALLET:P+001", "P-001")]
    [InlineData("PALLET:P+001æ", "P-001")]
    [InlineData("abcPALLET:P+0æ0*1xyz", "P-001")]
    [InlineData("___P+001???", "P-001")]
    [InlineData("P-001", "P-001")]
    [InlineData("P+001", "P-001")]
    public void TryParsePalletCode_WithSupportedInput_ReturnsNormalizedPalletId(string scannedValue, string expectedPalletId)
    {
        var success = WarehouseBarcode.TryParsePalletCode(scannedValue, out var palletId);

        Assert.True(success);
        Assert.Equal(expectedPalletId, palletId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("PALLET:")]
    [InlineData("PALLET:P-ABC")]
    [InlineData("ITEM:123")]
    public void TryParsePalletCode_WithInvalidInput_ReturnsFalse(string scannedValue)
    {
        var success = WarehouseBarcode.TryParsePalletCode(scannedValue, out var palletId);

        Assert.False(success);
        Assert.Equal(string.Empty, palletId);
    }

    [Theory]
    [InlineData("p+001", "PALLET:P-001")]
    [InlineData("P-00A1", "PALLET:P-00A1")]
    [InlineData("  p-123  ", "PALLET:P-123")]
    public void CreatePalletCode_NormalizesId(string palletId, string expectedCode)
    {
        var result = WarehouseBarcode.CreatePalletCode(palletId);

        Assert.Equal(expectedCode, result);
    }
}
