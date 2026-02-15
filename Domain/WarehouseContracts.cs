namespace LagerPalleSortering.Domain;

public sealed record PalletRecord(
    string PalletId,
    string GroupKey,
    string ProductNumber,
    string ExpiryDate,
    int TotalQuantity,
    bool IsClosed,
    DateTime CreatedAt);

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

public sealed record RegisterResult(
    bool Success,
    string? PalletId,
    string? ProductNumber,
    string? ExpiryDate,
    int Quantity,
    bool CreatedNewPallet,
    string Message)
{
    public static RegisterResult Ok(
        string palletId,
        string productNumber,
        string expiryDate,
        int quantity,
        bool createdNewPallet,
        string message) =>
        new(true, palletId, productNumber, expiryDate, quantity, createdNewPallet, message);

    public static RegisterResult Fail(string message) =>
        new(false, null, null, null, 0, false, message);
}

public sealed record MoveConfirmationResult(
    bool Success,
    string Message,
    string? PalletId,
    long? ScanEntryId)
{
    public static MoveConfirmationResult Ok(string message, string palletId, long scanEntryId) =>
        new(true, message, palletId, scanEntryId);

    public static MoveConfirmationResult Fail(string message) =>
        new(false, message, null, null);
}
