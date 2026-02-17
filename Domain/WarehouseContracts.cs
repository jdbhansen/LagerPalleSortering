namespace LagerPalleSortering.Domain;

/// <summary>
/// Aggregated pallet state used by dashboards and print views.
/// </summary>
public sealed record PalletRecord(
    string PalletId,
    string GroupKey,
    string ProductNumber,
    string ExpiryDate,
    int TotalQuantity,
    bool IsClosed,
    DateTime CreatedAt);

/// <summary>
/// One product line on a pallet content printout.
/// </summary>
public sealed record PalletContentItemRecord(
    string ProductNumber,
    string ExpiryDate,
    int Quantity);

/// <summary>
/// Immutable registration event including confirmation progress.
/// </summary>
public sealed record ScanEntryRecord(
    long Id,
    DateTime Timestamp,
    string ProductNumber,
    string ExpiryDate,
    int Quantity,
    string PalletId,
    string GroupKey,
    bool CreatedNewPallet,
    int ConfirmedQuantity,
    bool ConfirmedMoved,
    DateTime? ConfirmedAt);

public sealed record UndoResult(string PalletId, int Quantity);

/// <summary>
/// Audit trail entry for critical operator actions.
/// </summary>
public sealed record AuditEntryRecord(
    long Id,
    DateTime Timestamp,
    string Action,
    string Details,
    string MachineName);

/// <summary>
/// Lightweight operational snapshot used by health endpoint.
/// </summary>
public sealed record WarehouseHealthSnapshot(
    int OpenPallets,
    int OpenColli,
    int PendingConfirmations,
    DateTime? LastEntryTimestampUtc);

public sealed record RegisterResult(
    bool Success,
    string? PalletId,
    string? ProductNumber,
    string? ExpiryDate,
    int Quantity,
    bool CreatedNewPallet,
    string Message)
{
    // Convenience constructor for successful register outcome.
    public static RegisterResult Ok(
        string palletId,
        string productNumber,
        string expiryDate,
        int quantity,
        bool createdNewPallet,
        string message) =>
        new(true, palletId, productNumber, expiryDate, quantity, createdNewPallet, message);

    // Convenience constructor for failed register outcome.
    public static RegisterResult Fail(string message) =>
        new(false, null, null, null, 0, false, message);
}

public sealed record MoveConfirmationResult(
    bool Success,
    string Message,
    string? PalletId,
    long? ScanEntryId)
{
    // Convenience constructor for successful confirmation outcome.
    public static MoveConfirmationResult Ok(string message, string palletId, long scanEntryId) =>
        new(true, message, palletId, scanEntryId);

    // Convenience constructor for failed confirmation outcome.
    public static MoveConfirmationResult Fail(string message) =>
        new(false, message, null, null);
}

/// <summary>
/// Aggregated result for confirming multiple colli by repeated pallet scans.
/// </summary>
public sealed record MoveBatchConfirmationResult(
    string Status,
    string Message,
    string? PalletId,
    int Confirmed,
    int Requested)
{
    public static MoveBatchConfirmationResult Success(string message, string? palletId, int confirmed, int requested) =>
        new("success", message, palletId, confirmed, requested);

    public static MoveBatchConfirmationResult Warning(string message, string? palletId, int confirmed, int requested) =>
        new("warning", message, palletId, confirmed, requested);

    public static MoveBatchConfirmationResult Error(string message, string? palletId = null, int confirmed = 0, int requested = 0) =>
        new("error", message, palletId, confirmed, requested);
}
