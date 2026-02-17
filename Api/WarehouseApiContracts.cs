using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Api;

public sealed record RegisterColliApiRequest(string? ProductNumber, string? ExpiryDateRaw, int Quantity);

public sealed record ConfirmMoveApiRequest(string? ScannedPalletCode, int ConfirmScanCount);

public sealed record WarehouseDashboardApiResponse(List<PalletRecord> OpenPallets, List<ScanEntryRecord> Entries);

public sealed record WarehouseOperationApiResponse(
    string Type,
    string Message,
    string? PalletId = null,
    int Confirmed = 0,
    int Requested = 0);
