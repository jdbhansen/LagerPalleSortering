namespace LagerPalleSortering.Services;

public sealed record LagerHotkeysOptions(
    string ProductInputId,
    string PalletInputId,
    string RegisterButtonId,
    string ConfirmButtonId,
    string UndoButtonId,
    string ClearCancelButtonId);
