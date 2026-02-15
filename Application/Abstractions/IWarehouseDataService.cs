using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Abstractions;

public interface IWarehouseDataService
{
    Task InitializeAsync();
    Task<RegisterResult> RegisterColliAsync(string productNumber, string? expiryRaw, int quantity);
    Task<MoveConfirmationResult> ConfirmMoveByPalletScanAsync(string scannedPalletCode);
    Task ClosePalletAsync(string palletId);
    Task<UndoResult?> UndoLastAsync();
    Task<List<PalletRecord>> GetOpenPalletsAsync();
    Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries);
    Task<PalletRecord?> GetPalletForPrintAsync(string palletId);
}
