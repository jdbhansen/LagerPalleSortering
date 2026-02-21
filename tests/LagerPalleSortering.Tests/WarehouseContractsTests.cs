using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Tests;

public sealed class WarehouseContractsTests
{
    [Fact]
    public void RegisterResult_OkFactory_MapsAllFields()
    {
        var result = RegisterResult.Ok("P-001", "ITEM-1", "20270101", 2, true, "ok");

        Assert.True(result.Success);
        Assert.Equal("P-001", result.PalletId);
        Assert.Equal("ITEM-1", result.ProductNumber);
        Assert.Equal("20270101", result.ExpiryDate);
        Assert.Equal(2, result.Quantity);
        Assert.True(result.CreatedNewPallet);
        Assert.Equal("ok", result.Message);
    }

    [Fact]
    public void RegisterResult_FailFactory_MapsFailureDefaults()
    {
        var result = RegisterResult.Fail("fejl");

        Assert.False(result.Success);
        Assert.Null(result.PalletId);
        Assert.Null(result.ProductNumber);
        Assert.Null(result.ExpiryDate);
        Assert.Equal(0, result.Quantity);
        Assert.False(result.CreatedNewPallet);
        Assert.Equal("fejl", result.Message);
    }

    [Fact]
    public void AuditEntryRecord_ConstructsWithExpectedValues()
    {
        var timestamp = DateTime.UtcNow;
        var record = new AuditEntryRecord(42, timestamp, "REGISTER_COLLI", "details", "MACHINE-1");

        Assert.Equal(42, record.Id);
        Assert.Equal(timestamp, record.Timestamp);
        Assert.Equal("REGISTER_COLLI", record.Action);
        Assert.Equal("details", record.Details);
        Assert.Equal("MACHINE-1", record.MachineName);
    }
}
