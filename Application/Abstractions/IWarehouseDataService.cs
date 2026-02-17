using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Abstractions;

/// <summary>
/// Use-case facade for warehouse registration, confirmation and read models.
/// </summary>
public interface IWarehouseDataService
{
    /// <summary>
    /// Ensures persistence is initialized and migrations are applied.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Registers inbound colli and returns which pallet should receive the goods.
    /// </summary>
    Task<RegisterResult> RegisterColliAsync(string productNumber, string? expiryRaw, int quantity, CancellationToken cancellationToken = default);
    /// <summary>
    /// Confirms physical move by scanned pallet label.
    /// </summary>
    Task<MoveConfirmationResult> ConfirmMoveByPalletScanAsync(
        string scannedPalletCode,
        bool bypassDuplicateGuard = false,
        CancellationToken cancellationToken = default);
    Task<MoveBatchConfirmationResult> ConfirmMoveBatchByPalletScanAsync(
        string scannedPalletCode,
        int confirmScanCount,
        CancellationToken cancellationToken = default);
    Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default);
    Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default);
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);
    Task<byte[]> BackupDatabaseAsync(CancellationToken cancellationToken = default);
    Task RestoreDatabaseAsync(Stream databaseStream, CancellationToken cancellationToken = default);
    Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default);
    Task<List<PalletContentItemRecord>> GetPalletContentsAsync(string palletId, CancellationToken cancellationToken = default);
    Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default);
    Task<List<AuditEntryRecord>> GetRecentAuditEntriesAsync(int maxEntries, CancellationToken cancellationToken = default);
    Task<WarehouseHealthSnapshot> GetHealthSnapshotAsync(CancellationToken cancellationToken = default);
    Task<PalletRecord?> GetPalletForPrintAsync(string palletId, CancellationToken cancellationToken = default);
}
