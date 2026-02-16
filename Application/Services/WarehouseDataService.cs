using System.Globalization;
using System.Collections.Concurrent;
using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;
using Microsoft.Extensions.Options;

namespace LagerPalleSortering.Application.Services;

/// <summary>
/// Coordinates warehouse business rules between UI-facing use-cases and repository persistence.
/// </summary>
public sealed class WarehouseDataService : IWarehouseDataService
{
    private readonly IWarehouseRepository _repository;
    private readonly IProductBarcodeNormalizer _productBarcodeNormalizer;
    private readonly IPalletBarcodeService _palletBarcodeService;
    private readonly IOperationalMetrics _metrics;
    private readonly WarehouseRulesOptions _rules;
    private readonly ConcurrentDictionary<string, DateTime> _recentConfirmScans = new(StringComparer.Ordinal);

    public WarehouseDataService(
        IWarehouseRepository repository,
        IProductBarcodeNormalizer productBarcodeNormalizer,
        IPalletBarcodeService palletBarcodeService,
        IOperationalMetrics metrics,
        IOptions<WarehouseRulesOptions> rules)
    {
        _repository = repository;
        _productBarcodeNormalizer = productBarcodeNormalizer;
        _palletBarcodeService = palletBarcodeService;
        _metrics = metrics;
        _rules = rules.Value;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default) => _repository.InitializeAsync(cancellationToken);

    public async Task<RegisterResult> RegisterColliAsync(string productNumber, string? expiryRaw, int quantity, CancellationToken cancellationToken = default)
    {
        _metrics.IncrementRegisterAttempt();
        // Normalize scanner input before validation to keep grouping stable.
        var product = _productBarcodeNormalizer.Normalize(productNumber ?? string.Empty);
        var expiry = NormalizeExpiry(expiryRaw);

        if (string.IsNullOrWhiteSpace(product))
        {
            _metrics.IncrementRegisterFailure("Varenummer mangler.");
            return RegisterResult.Fail("Varenummer mangler.");
        }

        if (quantity <= 0)
        {
            _metrics.IncrementRegisterFailure("Antal kolli skal være større end 0.");
            return RegisterResult.Fail("Antal kolli skal være større end 0.");
        }

        if (!HasValidExpiryFormat(expiry))
        {
            _metrics.IncrementRegisterFailure("Holdbarhed skal være YYYYMMDD eller tom.");
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

    public async Task<MoveConfirmationResult> ConfirmMoveByPalletScanAsync(
        string scannedPalletCode,
        bool bypassDuplicateGuard = false,
        CancellationToken cancellationToken = default)
    {
        _metrics.IncrementConfirmAttempt();
        if (!_palletBarcodeService.TryParsePalletCode(scannedPalletCode, out var palletId))
        {
            _metrics.IncrementConfirmFailure("Ugyldig pallestregkode.");
            return MoveConfirmationResult.Fail("Ugyldig pallestregkode. Forventet format: PALLET:P-001.");
        }

        if (!bypassDuplicateGuard && IsDuplicateConfirmScanBlocked(palletId))
        {
            _metrics.IncrementDuplicateScanBlocked();
            return MoveConfirmationResult.Fail("Scan ignoreret: samme palle blev allerede scannet lige før.");
        }

        var confirmedId = await _repository.ConfirmLatestUnconfirmedByPalletIdAsync(palletId, DateTime.UtcNow, cancellationToken);
        if (confirmedId is null)
        {
            _metrics.IncrementConfirmFailure($"Ingen u-bekræftede kolli fundet for palle {palletId}.");
            return MoveConfirmationResult.Fail($"Ingen u-bekræftede kolli fundet for palle {palletId}.");
        }

        return MoveConfirmationResult.Ok($"Flytning bekræftet på palle {palletId}.", palletId, confirmedId.Value);
    }

    public async Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default)
    {
        var result = await _repository.UndoLastAsync(cancellationToken);
        if (result is not null)
        {
            _metrics.IncrementUndo();
        }

        return result;
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await _repository.ClearAllDataAsync(cancellationToken);
        _metrics.IncrementClearAll();
    }

    public Task<byte[]> BackupDatabaseAsync(CancellationToken cancellationToken = default) =>
        _repository.BackupDatabaseAsync(cancellationToken);

    public Task RestoreDatabaseAsync(Stream databaseStream, CancellationToken cancellationToken = default) =>
        _repository.RestoreDatabaseAsync(databaseStream, cancellationToken);

    public Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default) => _repository.GetOpenPalletsAsync(cancellationToken);

    public Task<List<PalletContentItemRecord>> GetPalletContentsAsync(string palletId, CancellationToken cancellationToken = default) => _repository.GetPalletContentsAsync(palletId, cancellationToken);

    public Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default) => _repository.GetRecentEntriesAsync(maxEntries, cancellationToken);

    public Task<List<AuditEntryRecord>> GetRecentAuditEntriesAsync(int maxEntries, CancellationToken cancellationToken = default) =>
        _repository.GetRecentAuditEntriesAsync(maxEntries, cancellationToken);

    public Task<WarehouseHealthSnapshot> GetHealthSnapshotAsync(CancellationToken cancellationToken = default) =>
        _repository.GetHealthSnapshotAsync(cancellationToken);

    public Task<PalletRecord?> GetPalletForPrintAsync(string palletId, CancellationToken cancellationToken = default) => _repository.GetPalletByIdAsync(palletId, cancellationToken);

    private static string NormalizeExpiry(string? raw)
    {
        // Empty expiry is intentionally stored as a dedicated marker value.
        var value = (raw ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? WarehouseConstants.NoExpiry : value;
    }

    private static bool HasValidExpiryFormat(string expiry)
    {
        if (expiry == WarehouseConstants.NoExpiry)
        {
            return true;
        }

        // Fast reject before calendar parsing.
        if (expiry.Length != 8 || !expiry.All(char.IsDigit))
        {
            return false;
        }

        // Enforce real calendar dates, not just numeric shape.
        return DateTime.TryParseExact(
            expiry,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
    }

    private bool IsDuplicateConfirmScanBlocked(string palletId)
    {
        if (!_rules.EnableDuplicateScanGuard || _rules.DuplicateScanWindowMs <= 0)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        var threshold = now.AddMilliseconds(-_rules.DuplicateScanWindowMs);
        if (_recentConfirmScans.TryGetValue(palletId, out var previous) && previous >= threshold)
        {
            _recentConfirmScans[palletId] = now;
            return true;
        }

        _recentConfirmScans[palletId] = now;

        // Lightweight cleanup to avoid unbounded growth.
        if (_recentConfirmScans.Count > 512)
        {
            foreach (var pair in _recentConfirmScans)
            {
                if (pair.Value < threshold)
                {
                    _recentConfirmScans.TryRemove(pair.Key, out _);
                }
            }
        }

        return false;
    }

}
