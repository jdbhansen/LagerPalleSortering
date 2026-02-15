using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Abstractions;

public interface IWarehouseDataService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<RegisterResult> RegisterColliAsync(string productNumber, string? expiryRaw, int quantity, CancellationToken cancellationToken = default);
    Task<MoveConfirmationResult> ConfirmMoveByPalletScanAsync(string scannedPalletCode, CancellationToken cancellationToken = default);
    Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default);
    Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default);
    Task ClearAllDataAsync(CancellationToken cancellationToken = default);
    Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default);
    Task<List<PalletContentItemRecord>> GetPalletContentsAsync(string palletId, CancellationToken cancellationToken = default);
    Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default);
    Task<PalletRecord?> GetPalletForPrintAsync(string palletId, CancellationToken cancellationToken = default);
}
