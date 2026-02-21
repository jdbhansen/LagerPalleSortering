using System.Collections.Concurrent;
using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;
using Microsoft.Extensions.Options;

namespace LagerPalleSortering.Application.Services;

public sealed class SlidingWindowDuplicateScanGuard : IDuplicateScanGuard
{
    private readonly ConcurrentDictionary<string, DateTime> _recentScans = new(StringComparer.Ordinal);
    private readonly WarehouseRulesOptions _rules;
    private readonly TimeProvider _timeProvider;

    public SlidingWindowDuplicateScanGuard(IOptions<WarehouseRulesOptions> rules, TimeProvider timeProvider)
    {
        _rules = rules.Value;
        _timeProvider = timeProvider;
    }

    public bool IsBlocked(string palletId)
    {
        if (!_rules.EnableDuplicateScanGuard || _rules.DuplicateScanWindowMs <= 0)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var threshold = now.AddMilliseconds(-_rules.DuplicateScanWindowMs);
        if (_recentScans.TryGetValue(palletId, out var previous) && previous >= threshold)
        {
            _recentScans[palletId] = now;
            return true;
        }

        _recentScans[palletId] = now;

        if (_recentScans.Count > 512)
        {
            foreach (var pair in _recentScans)
            {
                if (pair.Value < threshold)
                {
                    _recentScans.TryRemove(pair.Key, out _);
                }
            }
        }

        return false;
    }
}
