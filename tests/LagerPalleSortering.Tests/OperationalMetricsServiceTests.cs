using LagerPalleSortering.Application.Services;

namespace LagerPalleSortering.Tests;

public sealed class OperationalMetricsServiceTests
{
    [Fact]
    public void GetSnapshot_WithoutEvents_ReturnsZeroedCounters()
    {
        var service = new OperationalMetricsService();

        var snapshot = service.GetSnapshot();

        Assert.Equal(0, snapshot.RegisterAttempts);
        Assert.Equal(0, snapshot.RegisterFailures);
        Assert.Equal(0, snapshot.ConfirmAttempts);
        Assert.Equal(0, snapshot.ConfirmFailures);
        Assert.Equal(0, snapshot.DuplicateScanBlocked);
        Assert.Equal(0, snapshot.UndoActions);
        Assert.Equal(0, snapshot.ClearAllActions);
        Assert.Null(snapshot.LastErrorMessage);
        Assert.Null(snapshot.LastErrorAtUtc);
    }

    [Fact]
    public void IncrementMethods_UpdateCountersAndLastError()
    {
        var service = new OperationalMetricsService();

        service.IncrementRegisterAttempt();
        service.IncrementRegisterFailure("Register failed");
        service.IncrementConfirmAttempt();
        service.IncrementConfirmFailure("Confirm failed");
        service.IncrementDuplicateScanBlocked();
        service.IncrementUndo();
        service.IncrementClearAll();

        var snapshot = service.GetSnapshot();

        Assert.Equal(1, snapshot.RegisterAttempts);
        Assert.Equal(1, snapshot.RegisterFailures);
        Assert.Equal(1, snapshot.ConfirmAttempts);
        Assert.Equal(1, snapshot.ConfirmFailures);
        Assert.Equal(1, snapshot.DuplicateScanBlocked);
        Assert.Equal(1, snapshot.UndoActions);
        Assert.Equal(1, snapshot.ClearAllActions);
        Assert.Equal("Confirm failed", snapshot.LastErrorMessage);
        Assert.NotNull(snapshot.LastErrorAtUtc);
        Assert.True(snapshot.LastErrorAtUtc <= DateTime.UtcNow);
    }
}
