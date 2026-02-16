using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Services;

public sealed class WarehouseDataService : IWarehouseDataService
{
    private readonly IWarehouseRepository _repository;
    private readonly IProductBarcodeNormalizer _productBarcodeNormalizer;
    private readonly IPalletBarcodeService _palletBarcodeService;

    public WarehouseDataService(
        IWarehouseRepository repository,
        IProductBarcodeNormalizer productBarcodeNormalizer,
        IPalletBarcodeService palletBarcodeService)
    {
        _repository = repository;
        _productBarcodeNormalizer = productBarcodeNormalizer;
        _palletBarcodeService = palletBarcodeService;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => _repository.InitializeAsync(cancellationToken);

    public async Task<RegisterResult> RegisterColliAsync(string productNumber, string? expiryRaw, int quantity, CancellationToken cancellationToken = default)
    {
        var product = _productBarcodeNormalizer.Normalize(productNumber ?? string.Empty);
        var expiry = NormalizeExpiry(expiryRaw);

        if (string.IsNullOrWhiteSpace(product))
        {
            return RegisterResult.Fail("Varenummer mangler.");
        }

        if (quantity <= 0)
        {
            return RegisterResult.Fail("Antal kolli skal være større end 0.");
        }

        if (!HasValidExpiryFormat(expiry))
        {
            return RegisterResult.Fail("Holdbarhed skal være YYYYMMDD eller tom.");
        }

        var result = await _repository.RegisterAsync(product, expiry, quantity, cancellationToken);
        var actionText = result.CreatedNewPallet ? "Ny palle oprettet" : "Brug eksisterende palle";

        return RegisterResult.Ok(
            result.PalletId,
            product,
            expiry,
            quantity,
            result.CreatedNewPallet,
            $"{actionText}: læg kolli på {result.PalletId}.");
    }

    public Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default) => _repository.ClosePalletAsync(palletId, cancellationToken);

    public async Task<MoveConfirmationResult> ConfirmMoveByPalletScanAsync(string scannedPalletCode, CancellationToken cancellationToken = default)
    {
        if (!_palletBarcodeService.TryParsePalletCode(scannedPalletCode, out var palletId))
        {
            return MoveConfirmationResult.Fail("Ugyldig pallestregkode. Forventet format: PALLET:P-001.");
        }

        var confirmedId = await _repository.ConfirmLatestUnconfirmedByPalletIdAsync(palletId, DateTime.UtcNow, cancellationToken);
        if (confirmedId is null)
        {
            return MoveConfirmationResult.Fail($"Ingen u-bekræftede kolli fundet for palle {palletId}.");
        }

        return MoveConfirmationResult.Ok($"Flytning bekræftet på palle {palletId}.", palletId, confirmedId.Value);
    }

    public Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default) => _repository.UndoLastAsync(cancellationToken);

    public Task ClearAllDataAsync(CancellationToken cancellationToken = default) => _repository.ClearAllDataAsync(cancellationToken);

    public Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default) => _repository.GetOpenPalletsAsync(cancellationToken);

    public Task<List<PalletContentItemRecord>> GetPalletContentsAsync(string palletId, CancellationToken cancellationToken = default) => _repository.GetPalletContentsAsync(palletId, cancellationToken);

    public Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default) => _repository.GetRecentEntriesAsync(maxEntries, cancellationToken);

    public Task<PalletRecord?> GetPalletForPrintAsync(string palletId, CancellationToken cancellationToken = default) => _repository.GetPalletByIdAsync(palletId, cancellationToken);

    private static string NormalizeExpiry(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? WarehouseConstants.NoExpiry : value;
    }

    private static bool HasValidExpiryFormat(string expiry)
    {
        if (expiry == WarehouseConstants.NoExpiry)
        {
            return true;
        }

        return expiry.Length == 8 && expiry.All(char.IsDigit);
    }

}
