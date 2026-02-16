using System.Text;
using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Application.Services;
using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Tests;

public sealed class WarehouseExportServiceCsvTests
{
    [Fact]
    public async Task ExportCsvAsync_IncludesUtf8Bom_AndHeader()
    {
        var repository = new StubWarehouseRepository(
            [
                CreateEntry(1, DateTime.UtcNow, "ITEM-1", "P-001")
            ]);
        var service = new WarehouseExportService(repository);

        var bytes = await service.ExportCsvAsync();

        var bom = Encoding.UTF8.GetPreamble();
        Assert.True(bytes.Length >= bom.Length);
        Assert.True(bytes.Take(bom.Length).SequenceEqual(bom));

        var text = Encoding.UTF8.GetString(bytes);
        Assert.Contains("TimestampUtc,PalletId,ProductNumber,ExpiryDate,Quantity,ConfirmedQuantity,CreatedNewPallet,ConfirmedMoved,ConfirmedAtUtc", text);
    }

    [Fact]
    public async Task ExportCsvAsync_OrdersRowsByTimestampAscending()
    {
        var later = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var earlier = later.AddMinutes(-5);
        var repository = new StubWarehouseRepository(
            [
                CreateEntry(2, later, "LATE", "P-002", createdNewPallet: false),
                CreateEntry(1, earlier, "EARLY", "P-001")
            ]);
        var service = new WarehouseExportService(repository);

        var text = Encoding.UTF8.GetString(await service.ExportCsvAsync());

        var earlyIndex = text.IndexOf("EARLY", StringComparison.Ordinal);
        var lateIndex = text.IndexOf("LATE", StringComparison.Ordinal);
        Assert.True(earlyIndex >= 0);
        Assert.True(lateIndex >= 0);
        Assert.True(earlyIndex < lateIndex);
    }

    [Fact]
    public async Task ExportCsvAsync_EscapesCommaQuotesAndLineBreaks()
    {
        var repository = new StubWarehouseRepository(
            [
                CreateEntry(1, DateTime.UtcNow, "ITEM,\"A\"\nB", "P-001")
            ]);
        var service = new WarehouseExportService(repository);

        var text = Encoding.UTF8.GetString(await service.ExportCsvAsync());

        Assert.Contains("\"ITEM,\"\"A\"\"\nB\"", text, StringComparison.Ordinal);
    }

    private static ScanEntryRecord CreateEntry(
        long id,
        DateTime timestamp,
        string productNumber,
        string palletId,
        bool createdNewPallet = true) =>
        new(
            id,
            timestamp,
            productNumber,
            "20270101",
            1,
            palletId,
            $"{productNumber}|20270101",
            createdNewPallet,
            0,
            false,
            null);

    private sealed class StubWarehouseRepository : IWarehouseRepository
    {
        private readonly List<ScanEntryRecord> _entries;

        public StubWarehouseRepository(List<ScanEntryRecord> entries)
        {
            _entries = entries;
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<(string PalletId, bool CreatedNewPallet)> RegisterAsync(string productNumber, string expiryDate, int quantity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<long?> ConfirmLatestUnconfirmedByPalletIdAsync(string palletId, DateTime confirmedAtUtc, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task ClearAllDataAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<byte[]> BackupDatabaseAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task RestoreDatabaseAsync(Stream databaseStream, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<PalletRecord>());

        public Task<List<PalletContentItemRecord>> GetPalletContentsAsync(string palletId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<PalletContentItemRecord>());

        public Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.Take(maxEntries).ToList());

        public Task<List<AuditEntryRecord>> GetRecentAuditEntriesAsync(int maxEntries, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<AuditEntryRecord>());

        public Task<WarehouseHealthSnapshot> GetHealthSnapshotAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new WarehouseHealthSnapshot(0, 0, 0, null));

        public Task<PalletRecord?> GetPalletByIdAsync(string palletId, CancellationToken cancellationToken = default) =>
            Task.FromResult<PalletRecord?>(null);
    }
}
