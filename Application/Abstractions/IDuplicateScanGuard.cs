namespace LagerPalleSortering.Application.Abstractions;

/// <summary>
/// Guards against accidental duplicate pallet scans inside a short time window.
/// </summary>
public interface IDuplicateScanGuard
{
    bool IsBlocked(string palletId);
}
