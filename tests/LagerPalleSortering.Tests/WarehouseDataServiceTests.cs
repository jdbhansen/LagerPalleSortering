using System.Text;
using ClosedXML.Excel;
using LagerPalleSortering.Application.Services;
using LagerPalleSortering.Domain;
using LagerPalleSortering.Infrastructure.Repositories;
using LagerPalleSortering.Tests.TestInfrastructure;
using Microsoft.Extensions.Options;

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
    public async Task GetPalletContentsAsync_ReturnsProductExpiryAndQuantityRows()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var first = await fixture.Service.RegisterColliAsync("item-01", "20260101", 2);
        await fixture.Service.RegisterColliAsync("item-02", "20260202", 1);

        var contents = await fixture.Service.GetPalletContentsAsync(first.PalletId!);

        Assert.Equal(2, contents.Count);
        Assert.Contains(contents, x => x.ProductNumber == "ITEM-01" && x.ExpiryDate == "20260101" && x.Quantity == 2);
        Assert.Contains(contents, x => x.ProductNumber == "ITEM-02" && x.ExpiryDate == "20260202" && x.Quantity == 1);
    }

    [Fact]
    public async Task GetPalletContentsAsync_WhenPalletIsClosed_StillReturnsRows()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var register = await fixture.Service.RegisterColliAsync("item-10", "20260303", 3);
        await fixture.Service.ClosePalletAsync(register.PalletId!);

        var contents = await fixture.Service.GetPalletContentsAsync(register.PalletId!);

        Assert.Single(contents);
        Assert.Equal("ITEM-10", contents[0].ProductNumber);
        Assert.Equal("20260303", contents[0].ExpiryDate);
        Assert.Equal(3, contents[0].Quantity);
    }

    [Fact]
    public async Task GetPalletContentsAsync_UnknownPallet_ReturnsEmpty()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var contents = await fixture.Service.GetPalletContentsAsync("P-999");

        Assert.Empty(contents);
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
    public async Task RegisterColliAsync_WhitespaceExpiry_UsesNoExpiryBucket()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.RegisterColliAsync("item-01", "   ", 1);
        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.True(result.Success);
        Assert.Single(openPallets);
        Assert.Equal("NOEXP", openPallets[0].ExpiryDate);
    }

    [Fact]
    public async Task RegisterColliAsync_WhitespaceProduct_ReturnsValidationError()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.RegisterColliAsync("   ", "20260101", 1);
        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.False(result.Success);
        Assert.Contains("Varenummer mangler", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(openPallets);
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
    public async Task ClearAllDataAsync_RemovesAllRows_AndResetsPalletSequence()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);
        await fixture.Service.RegisterColliAsync("item-02", "20260101", 1);

        await fixture.Service.ClearAllDataAsync();

        var openPallets = await fixture.Service.GetOpenPalletsAsync();
        var entries = await fixture.Service.GetRecentEntriesAsync(10);
        var firstAfterClear = await fixture.Service.RegisterColliAsync("item-99", "20261224", 1);

        Assert.Empty(openPallets);
        Assert.Empty(entries);
        Assert.True(firstAfterClear.Success);
        Assert.Equal("P-001", firstAfterClear.PalletId);
    }

    [Fact]
    public async Task ClearAllDataAsync_RemovesPalletContents()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var created = await fixture.Service.RegisterColliAsync("item-20", "20261111", 2);
        var before = await fixture.Service.GetPalletContentsAsync(created.PalletId!);
        await fixture.Service.ClearAllDataAsync();
        var after = await fixture.Service.GetPalletContentsAsync(created.PalletId!);

        Assert.Single(before);
        Assert.Empty(after);
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
    public async Task RegisterColliAsync_InvalidCalendarExpiry_ReturnsValidationError()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.RegisterColliAsync("item-01", "20260230", 1);
        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.False(result.Success);
        Assert.Contains("YYYYMMDD", result.Message);
        Assert.Empty(openPallets);
    }

    [Fact]
    public async Task GetOpenPalletsAsync_SortsByNumericPalletNumber()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        for (var day = 1; day <= 12; day++)
        {
            var expiry = $"202601{day:00}";
            await fixture.Service.RegisterColliAsync("ORDER-ITEM", expiry, 1);
        }

        var openPallets = await fixture.Service.GetOpenPalletsAsync();

        Assert.Equal(12, openPallets.Count);
        for (var index = 1; index <= openPallets.Count; index++)
        {
            Assert.Equal($"P-{index:000}", openPallets[index - 1].PalletId);
        }
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
    public async Task ConfirmMoveByPalletScanAsync_WhenScannerReplacesHyphenWithPlus_StillConfirms()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        var register = await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);

        var plusEncodedPalletCode = $"PALLET:{register.PalletId}".Replace("-", "+", StringComparison.Ordinal);
        var confirm = await fixture.Service.ConfirmMoveByPalletScanAsync(plusEncodedPalletCode);
        var entries = await fixture.Service.GetRecentEntriesAsync(1);

        Assert.True(confirm.Success);
        Assert.Equal(register.PalletId, confirm.PalletId);
        Assert.Single(entries);
        Assert.True(entries[0].ConfirmedMoved);
    }

    [Fact]
    public async Task ConfirmMoveByPalletScanAsync_WhenScannerAddsDanishCharacter_StillConfirms()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        var register = await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);

        var noisyPalletCode = $"PALLET:{register.PalletId}".Replace("-", "+", StringComparison.Ordinal) + "æ";
        var confirm = await fixture.Service.ConfirmMoveByPalletScanAsync(noisyPalletCode);
        var entries = await fixture.Service.GetRecentEntriesAsync(1);

        Assert.True(confirm.Success);
        Assert.Equal(register.PalletId, confirm.PalletId);
        Assert.Single(entries);
        Assert.True(entries[0].ConfirmedMoved);
    }

    [Fact]
    public async Task ConfirmMoveByPalletScanAsync_WhenScannerMapsColonToDanishAe_StillConfirms()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        var register = await fixture.Service.RegisterColliAsync("item-01", "20260101", 1);

        var mappedSeparatorCode = $"PALLET:{register.PalletId}".Replace(":", "æ", StringComparison.Ordinal);
        var confirm = await fixture.Service.ConfirmMoveByPalletScanAsync(mappedSeparatorCode);
        var entries = await fixture.Service.GetRecentEntriesAsync(1);

        Assert.True(confirm.Success);
        Assert.Equal(register.PalletId, confirm.PalletId);
        Assert.Single(entries);
        Assert.True(entries[0].ConfirmedMoved);
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
    public async Task ConfirmMoveBatchByPalletScanAsync_WhenFullyConfirmed_ReturnsSuccessStatus()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        var register = await fixture.Service.RegisterColliAsync("item-02", "20260101", 2);

        var result = await fixture.Service.ConfirmMoveBatchByPalletScanAsync($"PALLET:{register.PalletId}", 2);
        var entries = await fixture.Service.GetRecentEntriesAsync(1);

        Assert.Equal("success", result.Status);
        Assert.Equal(2, result.Confirmed);
        Assert.Equal(2, result.Requested);
        Assert.Single(entries);
        Assert.True(entries[0].ConfirmedMoved);
        Assert.Equal(2, entries[0].ConfirmedQuantity);
    }

    [Fact]
    public async Task ConfirmMoveBatchByPalletScanAsync_WhenPartiallyConfirmed_ReturnsWarningStatus()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        var register = await fixture.Service.RegisterColliAsync("item-03", "20260101", 1);

        var result = await fixture.Service.ConfirmMoveBatchByPalletScanAsync($"PALLET:{register.PalletId}", 2);
        var entries = await fixture.Service.GetRecentEntriesAsync(1);

        Assert.Equal("warning", result.Status);
        Assert.Equal(1, result.Confirmed);
        Assert.Equal(2, result.Requested);
        Assert.Single(entries);
        Assert.True(entries[0].ConfirmedMoved);
        Assert.Equal(1, entries[0].ConfirmedQuantity);
    }

    [Fact]
    public async Task ConfirmMoveBatchByPalletScanAsync_WhenCountIsNonPositive_ReturnsErrorStatus()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.ConfirmMoveBatchByPalletScanAsync("PALLET:P-001", 0);

        Assert.Equal("error", result.Status);
        Assert.Equal(0, result.Confirmed);
        Assert.Equal(0, result.Requested);
        Assert.Contains("større end 0", result.Message);
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
    public async Task ConfirmMoveByPalletScanAsync_WellFormedButUnknownPallet_ReturnsNoUnconfirmedForParsedPalletId()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");

        var result = await fixture.Service.ConfirmMoveByPalletScanAsync("PALLET:P-321");

        Assert.False(result.Success);
        Assert.Contains("P-321", result.Message, StringComparison.Ordinal);
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

    [Fact]
    public async Task BackupAndRestoreDatabase_RestoresOriginalState()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        await fixture.Service.RegisterColliAsync("BACKUP-1", "20261224", 2);
        var backup = await fixture.Service.BackupDatabaseAsync();

        await fixture.Service.ClearAllDataAsync();
        var empty = await fixture.Service.GetOpenPalletsAsync();
        Assert.Empty(empty);

        await using var stream = new MemoryStream(backup);
        await fixture.Service.RestoreDatabaseAsync(stream);
        var restored = await fixture.Service.GetOpenPalletsAsync();

        Assert.Single(restored);
        Assert.Equal("BACKUP-1", restored[0].ProductNumber);
    }

    [Fact]
    public async Task CriticalActions_WriteAuditEntries()
    {
        using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringTests");
        var register = await fixture.Service.RegisterColliAsync("AUDIT-1", "20261224", 1);

        await fixture.Service.ClosePalletAsync(register.PalletId!);
        await fixture.Service.UndoLastAsync();

        var audits = await fixture.Service.GetRecentAuditEntriesAsync(20);
        Assert.Contains(audits, x => x.Action == "REGISTER_COLLI");
        Assert.Contains(audits, x => x.Action == "CLOSE_PALLET");
        Assert.Contains(audits, x => x.Action == "UNDO_LAST");
    }

    [Fact]
    public async Task ConfirmMoveByPalletScanAsync_DuplicateGuard_BlocksRapidDuplicateScan()
    {
        var root = Path.Combine(Path.GetTempPath(), "LagerPalleSorteringTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var repository = new SqliteWarehouseRepository(
                new SqliteWarehouseDatabaseProvider(new TestWebHostEnvironment(root)),
                Options.Create(new WarehouseRulesOptions
                {
                    EnableDuplicateScanGuard = true,
                    DuplicateScanWindowMs = 5_000
                }));
            var service = new WarehouseDataService(
                repository,
                new DefaultProductBarcodeNormalizer(),
                new DefaultPalletBarcodeService(),
                new OperationalMetricsService(),
                Options.Create(new WarehouseRulesOptions
                {
                    EnableDuplicateScanGuard = true,
                    DuplicateScanWindowMs = 5_000
                }));
            await service.InitializeAsync();
            var register = await service.RegisterColliAsync("DUP-1", "20270101", 2);

            var first = await service.ConfirmMoveByPalletScanAsync($"PALLET:{register.PalletId}");
            var second = await service.ConfirmMoveByPalletScanAsync($"PALLET:{register.PalletId}");

            Assert.True(first.Success);
            Assert.False(second.Success);
            Assert.Contains("ignoreret", second.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
            }
        }
    }

}
