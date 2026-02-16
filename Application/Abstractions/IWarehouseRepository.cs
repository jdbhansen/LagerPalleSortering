using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Abstractions;

/// <summary>
/// Persistence contract for pallet and scan-entry state transitions.
/// </summary>
public interface IWarehouseRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Stores one registration and returns pallet selection outcome.
    /// </summary>
    Task<(string PalletId, bool CreatedNewPallet)> RegisterAsync(string productNumber, string expiryDate, int quantity, CancellationToken cancellationToken = default);
    Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Confirms the most recent unconfirmed colli for a pallet.
    /// </summary>
    Task<long?> ConfirmLatestUnconfirmedByPalletIdAsync(string palletId, DateTime confirmedAtUtc, CancellationToken cancellationToken = default);
    Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default);
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);
    Task<byte[]> BackupDatabaseAsync(CancellationToken cancellationToken = default);
    Task RestoreDatabaseAsync(Stream databaseStream, CancellationToken cancellationToken = default);
    Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default);
    Task<List<PalletContentItemRecord>> GetPalletContentsAsync(string palletId, CancellationToken cancellationToken = default);
    Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default);
    Task<List<AuditEntryRecord>> GetRecentAuditEntriesAsync(int maxEntries, CancellationToken cancellationToken = default);
    Task<WarehouseHealthSnapshot> GetHealthSnapshotAsync(CancellationToken cancellationToken = default);
    Task<PalletRecord?> GetPalletByIdAsync(string palletId, CancellationToken cancellationToken = default);
}
