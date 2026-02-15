using System.Text;
using ClosedXML.Excel;
using LagerPalleSortering.Tests.TestInfrastructure;

namespace LagerPalleSortering.Tests;

public sealed class WarehouseDataServiceTests
{
    [Fact]
    public async Task RegisterColliAsync_SameProductAndExpiry_ReusesSamePallet()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var first = await fixture.Service.RegisterColliAsync("item-01", "20260101", 2);
        var second = await fixture.Service.RegisterColliAsync("item-01", "20260101", 3);
        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.True(first.Success);
        Assert.True(first.CreatedNewPallet);
        Assert.True(second.Success);
        Assert.False(second.CreatedNewPallet);
        Assert.Equal(first.PalletId, second.PalletId);
        Assert.Single(openPallets);
        Assert.Equal(5, openPallets[0].TotalQuantity);
    }

    [Fact]
    public async Task RegisterColliAsync_DifferentExpiry_CreatesDifferentPallet()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var first = await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);
        var second = await fixture.Service.RegisterColliAsync("item-01", "20260102", 1);
        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.NotEqual(first.PalletId, second.PalletId);
        Assert.Equal(2, openPallets.Count);
    }

    [Fact]
    public async Task RegisterColliAsync_EmptyExpiry_UsesNoExpiryBucket()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.RegisterColliAsync("item-01", string.Empty, 1);
        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.True(result.Success);
        Assert.Single(openPallets);
        Assert.Equal("NOEXP", openPallets[0].ExpiryDate);
    }

    [Fact]
    public async Task ClosePalletAsync_ThenRegisterSameGroup_CreatesNewPallet()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var first = await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);
        await fixture.Service.ClosePalletAsync(first.PalletId!);
        var second = await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.NotEqual(first.PalletId, second.PalletId);
    }

    [Fact]
    public async Task UndoLastAsync_WhenFirstEntry_RemovesPallet()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var register = await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);
        var undo = await fixture.Service.UndoLastAsync();
        var openPallets = await fixture.Service.GetOpenPalletsAsync();
        var entries = await fixture.Service.GetRecentEntriesAsync(10);

        Assert.True(register.Success);
        Assert.NotNull(undo);
        Assert.Empty(openPallets);
        Assert.Empty(entries);
    }

    [Fact]
    public async Task DataPersistsAcrossServiceInstances()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var first = await fixture.Service.RegisterColliAsync("item-99", "20261224", 4);
        Assert.True(first.Success);

        var secondService = await fixture.CreateNewServiceForSameStorageAsync();
        var pallets = await secondService.GetOpenPalletsAsync();

        Assert.Single(pallets);
        Assert.Equal(first.PalletId, pallets[0].PalletId);
        Assert.Equal(4, pallets[0].TotalQuantity);
    }

    [Fact]
    public async Task ExportCsvAndExcel_ReturnExpectedContent()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        await fixture.Service.RegisterColliAsync("csv-item", "20270101", 2);

        var csv = await fixture.ExportService.ExportCsvAsync();
        var csvText = Encoding.UTF8.GetString(csv);
        Assert.Contains("TimestampUtc,PalletId,ProductNumber,ExpiryDate,Quantity,ConfirmedQuantity,CreatedNewPallet,ConfirmedMoved,ConfirmedAtUtc", csvText);
        Assert.Contains("csv-item", csvText, StringComparison.OrdinalIgnoreCase);

        var excel = await fixture.ExportService.ExportExcelAsync();
        using var stream = new MemoryStream(excel);
        using var workbook = new XLWorkbook(stream);
        Assert.NotNull(workbook.Worksheet("OpenPallets"));
        Assert.NotNull(workbook.Worksheet("ScanEntries"));
    }

    [Fact]
    public async Task RegisterColliAsync_InvalidExpiry_ReturnsValidationError()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.RegisterColliAsync("item-01", "2026-01-01", 1);
        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.False(result.Success);
        Assert.Contains("YYYYMMDD", result.Message);
        Assert.Empty(openPallets);
    }

    [Fact]
    public async Task RegisterColliAsync_TrimsAndUppercasesProductNumber()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.RegisterColliAsync("  item-abc  ", "20260101", 1);
        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.True(result.Success);
        Assert.Single(openPallets);
        Assert.Equal("ITEM-ABC", openPallets[0].ProductNumber);
    }

    [Fact]
    public async Task ConfirmMoveByPalletScanAsync_MarksLatestEntryConfirmed()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        var register = await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);

        var confirm = await fixture.Service.ConfirmMoveByPalletScanAsync($"PALLET:{register.PalletId}");
        var entries = await fixture.Service.GetRecentEntriesAsync(1);

        Assert.True(confirm.Success);
        Assert.Single(entries);
        Assert.True(entries[0].ConfirmedMoved);
        Assert.Equal(1, entries[0].ConfirmedQuantity);
        Assert.NotNull(entries[0].ConfirmedAt);
    }

    [Fact]
    public async Task ConfirmMoveByPalletScanAsync_QuantityTwo_RequiresTwoScans()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        var register = await fixture.Service.RegisterColliAsync("item-02", "20260101", 2);

        var firstConfirm = await fixture.Service.ConfirmMoveByPalletScanAsync($"PALLET:{register.PalletId}");
        var afterFirst = await fixture.Service.GetRecentEntriesAsync(1);
        var secondConfirm = await fixture.Service.ConfirmMoveByPalletScanAsync($"PALLET:{register.PalletId}");
        var afterSecond = await fixture.Service.GetRecentEntriesAsync(1);

        Assert.True(firstConfirm.Success);
        Assert.Single(afterFirst);
        Assert.False(afterFirst[0].ConfirmedMoved);
        Assert.Equal(1, afterFirst[0].ConfirmedQuantity);

        Assert.True(secondConfirm.Success);
        Assert.Single(afterSecond);
        Assert.True(afterSecond[0].ConfirmedMoved);
        Assert.Equal(2, afterSecond[0].ConfirmedQuantity);
    }

    [Fact]
    public async Task ConfirmMoveByPalletScanAsync_InvalidCode_ReturnsError()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);

        var result = await fixture.Service.ConfirmMoveByPalletScanAsync("ITEM:item-01");

        Assert.False(result.Success);
        Assert.Contains("PALLET:", result.Message);
    }

    [Fact]
    public async Task RegisterColliAsync_AllowsMaximumFourDifferentVariantsPerPallet()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var a = await fixture.Service.RegisterColliAsync("A", "20260101", 1);
        var b = await fixture.Service.RegisterColliAsync("B", "20260101", 1);
        var c = await fixture.Service.RegisterColliAsync("C", "20260101", 1);
        var d = await fixture.Service.RegisterColliAsync("D", "20260101", 1);
        var e = await fixture.Service.RegisterColliAsync("E", "20260101", 1);

        Assert.True(a.Success && b.Success && c.Success && d.Success && e.Success);
        Assert.Equal(a.PalletId, b.PalletId);
        Assert.Equal(a.PalletId, c.PalletId);
        Assert.Equal(a.PalletId, d.PalletId);
        Assert.NotEqual(a.PalletId, e.PalletId);
    }

    [Fact]
    public async Task RegisterColliAsync_DoesNotMixSameBarcodeWithDifferentExpiryOnSamePallet()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var first = await fixture.Service.RegisterColliAsync("BAR123", "20260101", 1);
        await fixture.Service.RegisterColliAsync("X1", "20260101", 1);
        await fixture.Service.RegisterColliAsync("X2", "20260101", 1);
        var secondExpiry = await fixture.Service.RegisterColliAsync("BAR123", "20260102", 1);

        Assert.True(first.Success);
        Assert.True(secondExpiry.Success);
        Assert.NotEqual(first.PalletId, secondExpiry.PalletId);
    }

    [Fact]
    public async Task RegisterColliAsync_Ean13WithScannerPrefix_IsNormalized()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.RegisterColliAsync("]E05701234567892", "20260101", 1);
        var pallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.True(result.Success);
        Assert.Single(pallets);
        Assert.Equal("5701234567892", pallets[0].ProductNumber);
    }

    [Fact]
    public async Task RegisterColliAsync_UpcA_AndEquivalentEan13_GroupTogether()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        // UPC-A: 036000291452 -> EAN-13 equivalent: 0036000291452
        var first = await fixture.Service.RegisterColliAsync("036000291452", "20260101", 1);
        var second = await fixture.Service.RegisterColliAsync("0036000291452", "20260101", 1);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(first.PalletId, second.PalletId);
    }

    [Fact]
    public async Task RegisterColliAsync_Ean8_IsAccepted()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.RegisterColliAsync("73513537", "20260101", 1);
        var pallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.True(result.Success);
        Assert.Single(pallets);
        Assert.Equal("73513537", pallets[0].ProductNumber);
    }

}
