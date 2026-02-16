using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Tests;

public sealed class WarehouseBarcodeTests
{
    [Theory]
    [InlineData("PALLET:P-001", "P-001")]
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
}
