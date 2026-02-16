namespace LagerPalleSortering.Domain;

/// <summary>
/// Runtime-tunable business rules and scan guardrails.
/// </summary>
public sealed class WarehouseRulesOptions
{
    public const string SectionName = "WarehouseRules";

    /// <summary>
    /// Maximum unique product+expiry variants allowed on one pallet.
    /// </summary>
    public int MaxVariantsPerPallet { get; set; } = 4;

    /// <summary>
    /// When enabled, rejects accidental duplicate pallet scans in short intervals.
    /// </summary>
    public bool EnableDuplicateScanGuard { get; set; } = true;

    /// <summary>
    /// Duplicate-scan suppression window in milliseconds.
    /// </summary>
    public int DuplicateScanWindowMs { get; set; } = 1200;
}
