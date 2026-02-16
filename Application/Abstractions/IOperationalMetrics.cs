namespace LagerPalleSortering.Application.Abstractions;

public interface IOperationalMetrics
{
    void IncrementRegisterAttempt();
    void IncrementRegisterFailure(string message);
    void IncrementConfirmAttempt();
    void IncrementConfirmFailure(string message);
    void IncrementDuplicateScanBlocked();
    void IncrementUndo();
    void IncrementClearAll();
    OperationalMetricsSnapshot GetSnapshot();
}

public sealed record OperationalMetricsSnapshot(
    long RegisterAttempts,
    long RegisterFailures,
    long ConfirmAttempts,
    long ConfirmFailures,
    long DuplicateScanBlocked,
    long UndoActions,
    long ClearAllActions,
    string? LastErrorMessage,
    DateTime? LastErrorAtUtc);
