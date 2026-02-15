using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Abstractions;

public interface IWarehouseRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task<(string PalletId, bool CreatedNewPallet)> RegisterAsync(string productNumber, string expiryDate, int quantity, CancellationToken cancellationToken = default);
    Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default);
    Task<long?> ConfirmLatestUnconfirmedByPalletIdAsync(string palletId, DateTime confirmedAtUtc, CancellationToken cancellationToken = default);
    Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default);
    Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default);
    Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default);
    Task<PalletRecord?> GetPalletByIdAsync(string palletId, CancellationToken cancellationToken = default);
}
