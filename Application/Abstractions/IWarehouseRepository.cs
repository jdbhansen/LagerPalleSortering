using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Abstractions;

public interface IWarehouseRepository
{
    Task InitializeAsync();
    Task<(string PalletId, bool CreatedNewPallet)> RegisterAsync(string productNumber, string expiryDate, int quantity);
    Task ClosePalletAsync(string palletId);
    Task<long?> ConfirmLatestUnconfirmedByPalletIdAsync(string palletId, DateTime confirmedAtUtc);
    Task<UndoResult?> UndoLastAsync();
    Task<List<PalletRecord>> GetOpenPalletsAsync();
    Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries);
    Task<PalletRecord?> GetPalletByIdAsync(string palletId);
}
