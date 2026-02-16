using System.Threading;
using LagerPalleSortering.Application.Abstractions;

namespace LagerPalleSortering.Application.Services;

public sealed class OperationalMetricsService : IOperationalMetrics
{
    private long _registerAttempts;
    private long _registerFailures;
    private long _confirmAttempts;
    private long _confirmFailures;
    private long _duplicateScanBlocked;
    private long _undoActions;
    private long _clearAllActions;

    private readonly Lock _errorLock = new();
    private string? _lastErrorMessage;
    private DateTime? _lastErrorAtUtc;

    public void IncrementRegisterAttempt() => Interlocked.Increment(ref _registerAttempts);

    public void IncrementRegisterFailure(string message)
    {
        Interlocked.Increment(ref _registerFailures);
        SetLastError(message);
    }

    public void IncrementConfirmAttempt() => Interlocked.Increment(ref _confirmAttempts);

    public void IncrementConfirmFailure(string message)
    {
        Interlocked.Increment(ref _confirmFailures);
        SetLastError(message);
    }

    public void IncrementDuplicateScanBlocked() => Interlocked.Increment(ref _duplicateScanBlocked);

    public void IncrementUndo() => Interlocked.Increment(ref _undoActions);

    public void IncrementClearAll() => Interlocked.Increment(ref _clearAllActions);

    public OperationalMetricsSnapshot GetSnapshot()
    {
        lock (_errorLock)
        {
            return new OperationalMetricsSnapshot(
                Interlocked.Read(ref _registerAttempts),
                Interlocked.Read(ref _registerFailures),
                Interlocked.Read(ref _confirmAttempts),
                Interlocked.Read(ref _confirmFailures),
                Interlocked.Read(ref _duplicateScanBlocked),
                Interlocked.Read(ref _undoActions),
                Interlocked.Read(ref _clearAllActions),
                _lastErrorMessage,
                _lastErrorAtUtc);
        }
    }

    private void SetLastError(string message)
    {
        lock (_errorLock)
        {
            _lastErrorMessage = message;
            _lastErrorAtUtc = DateTime.UtcNow;
        }
    }
}
