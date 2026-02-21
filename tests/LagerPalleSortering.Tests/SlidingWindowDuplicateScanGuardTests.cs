using LagerPalleSortering.Application.Services;
using LagerPalleSortering.Domain;
using Microsoft.Extensions.Options;

namespace LagerPalleSortering.Tests;

public sealed class SlidingWindowDuplicateScanGuardTests
{
    [Fact]
    public void IsBlocked_WhenGuardDisabled_ReturnsFalse()
    {
        var rules = Options.Create(new WarehouseRulesOptions
        {
            EnableDuplicateScanGuard = false,
            DuplicateScanWindowMs = 5_000
        });
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var guard = new SlidingWindowDuplicateScanGuard(rules, timeProvider);

        var first = guard.IsBlocked("P-001");
        var second = guard.IsBlocked("P-001");

        Assert.False(first);
        Assert.False(second);
    }

    [Fact]
    public void IsBlocked_WhenScannedInsideWindow_ReturnsTrueForSecondScan()
    {
        var rules = Options.Create(new WarehouseRulesOptions
        {
            EnableDuplicateScanGuard = true,
            DuplicateScanWindowMs = 5_000
        });
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var guard = new SlidingWindowDuplicateScanGuard(rules, timeProvider);

        var first = guard.IsBlocked("P-001");
        timeProvider.Advance(TimeSpan.FromSeconds(2));
        var second = guard.IsBlocked("P-001");

        Assert.False(first);
        Assert.True(second);
    }

    [Fact]
    public void IsBlocked_WhenWindowHasExpired_AllowsNewScan()
    {
        var rules = Options.Create(new WarehouseRulesOptions
        {
            EnableDuplicateScanGuard = true,
            DuplicateScanWindowMs = 5_000
        });
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var guard = new SlidingWindowDuplicateScanGuard(rules, timeProvider);

        var first = guard.IsBlocked("P-001");
        timeProvider.Advance(TimeSpan.FromSeconds(6));
        var second = guard.IsBlocked("P-001");

        Assert.False(first);
        Assert.False(second);
    }

    [Fact]
    public void IsBlocked_WhenDifferentPalletsAreScanned_DoesNotCrossBlock()
    {
        var rules = Options.Create(new WarehouseRulesOptions
        {
            EnableDuplicateScanGuard = true,
            DuplicateScanWindowMs = 5_000
        });
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var guard = new SlidingWindowDuplicateScanGuard(rules, timeProvider);

        var first = guard.IsBlocked("P-001");
        var other = guard.IsBlocked("P-002");

        Assert.False(first);
        Assert.False(other);
    }

    [Fact]
    public void IsBlocked_WhenCacheExceedsLimit_PerformsCleanupWithoutBlockingNewScan()
    {
        var rules = Options.Create(new WarehouseRulesOptions
        {
            EnableDuplicateScanGuard = true,
            DuplicateScanWindowMs = 1_000
        });
        var timeProvider = new MutableTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var guard = new SlidingWindowDuplicateScanGuard(rules, timeProvider);

        for (var i = 0; i < 520; i++)
        {
            Assert.False(guard.IsBlocked($"P-{i:000}"));
        }

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        var result = guard.IsBlocked("P-999");

        Assert.False(result);
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public MutableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public void Advance(TimeSpan delta)
        {
            _utcNow = _utcNow.Add(delta);
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
