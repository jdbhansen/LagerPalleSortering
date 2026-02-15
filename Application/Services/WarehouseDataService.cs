using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Services;

public sealed class WarehouseDataService : IWarehouseDataService
{
    private readonly IWarehouseRepository repository;

    public WarehouseDataService(IWarehouseRepository repository)
    {
        this.repository = repository;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => repository.InitializeAsync(cancellationToken);

    public async Task<RegisterResult> RegisterColliAsync(string productNumber, string? expiryRaw, int quantity, CancellationToken cancellationToken = default)
    {
        var product = ProductBarcodeParser.Normalize(productNumber ?? string.Empty);
        var expiry = NormalizeExpiry(expiryRaw);

        if (string.IsNullOrWhiteSpace(product))
        {
            return RegisterResult.Fail("Varenummer mangler.");
        }

        if (quantity <= 0)
        {
            return RegisterResult.Fail("Antal kolli skal være større end 0.");
        }

        if (!IsValidExpiry(expiry))
        {
            return RegisterResult.Fail("Holdbarhed skal være YYYYMMDD eller tom.");
        }

        var result = await repository.RegisterAsync(product, expiry, quantity, cancellationToken);
        var actionText = result.CreatedNewPallet ? "Ny palle oprettet" : "Brug eksisterende palle";

        return RegisterResult.Ok(
            result.PalletId,
            product,
            expiry,
            quantity,
            result.CreatedNewPallet,
            $"{actionText}: læg kolli på {result.PalletId}.");
    }

    public Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default) => repository.ClosePalletAsync(palletId, cancellationToken);

    public async Task<MoveConfirmationResult> ConfirmMoveByPalletScanAsync(string scannedPalletCode, CancellationToken cancellationToken = default)
    {
        if (!WarehouseBarcode.TryParsePalletCode(scannedPalletCode, out var palletId))
        {
            return MoveConfirmationResult.Fail("Ugyldig pallestregkode. Forventet format: PALLET:P-001.");
        }

        var confirmedId = await repository.ConfirmLatestUnconfirmedByPalletIdAsync(palletId, DateTime.UtcNow, cancellationToken);
        if (confirmedId is null)
        {
            return MoveConfirmationResult.Fail($"Ingen u-bekræftede kolli fundet for palle {palletId}.");
        }

        return MoveConfirmationResult.Ok($"Flytning bekræftet på palle {palletId}.", palletId, confirmedId.Value);
    }

    public Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default) => repository.UndoLastAsync(cancellationToken);

    public Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default) => repository.GetOpenPalletsAsync(cancellationToken);

    public Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default) => repository.GetRecentEntriesAsync(maxEntries, cancellationToken);

    public Task<PalletRecord?> GetPalletForPrintAsync(string palletId, CancellationToken cancellationToken = default) => repository.GetPalletByIdAsync(palletId, cancellationToken);

    private static string NormalizeExpiry(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? WarehouseConstants.NoExpiry : value;
    }

    private static bool IsValidExpiry(string expiry)
    {
        if (expiry == WarehouseConstants.NoExpiry)
        {
            return true;
        }

        return expiry.Length == 8 && expiry.All(char.IsDigit);
    }

}
