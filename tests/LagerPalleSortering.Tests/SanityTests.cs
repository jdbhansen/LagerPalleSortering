using System.Text;
using LagerPalleSortering.Tests.TestInfrastructure;

namespace LagerPalleSortering.Tests;

public sealed class SanityTests
{
    [Fact]
    [Trait("Category", "Sanity")]
    public async Task EmptyDatabase_HasNoOpenPallets()
    {
        await using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringSanity");
        var pallets = await fixture.Service.GetOpenPalletsAsync();
        Assert.Empty(pallets);
    }

    [Fact]
    [Trait("Category", "Sanity")]
    public async Task HappyPath_RegisterConfirmAndExportCsv_Works()
    {
        await using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringSanity");

        var register = await fixture.Service.RegisterColliAsync("SANITY-1", "20261231", 2);
        var confirm = await fixture.Service.ConfirmMoveByPalletScanAsync($"PALLET:{register.PalletId}");
        var csv = await fixture.ExportService.ExportCsvAsync();
        var csvText = Encoding.UTF8.GetString(csv);

        Assert.True(register.Success);
        Assert.True(confirm.Success);
        Assert.Contains("SANITY-1", csvText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Sanity")]
    public async Task Guardrails_MaxFourVariants_AndBarcodeDateConflict_AreEnforced()
    {
        await using var fixture = await WarehouseTestFixture.CreateAsync("LagerPalleSorteringSanity");

        var a = await fixture.Service.RegisterColliAsync("A", "20260101", 1);
        var b = await fixture.Service.RegisterColliAsync("B", "20260101", 1);
        var c = await fixture.Service.RegisterColliAsync("C", "20260101", 1);
        var d = await fixture.Service.RegisterColliAsync("D", "20260101", 1);
        var e = await fixture.Service.RegisterColliAsync("E", "20260101", 1);
        var x1 = await fixture.Service.RegisterColliAsync("BAR-1", "20260101", 1);
        var x2 = await fixture.Service.RegisterColliAsync("BAR-1", "20260102", 1);

        Assert.Equal(a.PalletId, b.PalletId);
        Assert.Equal(a.PalletId, c.PalletId);
        Assert.Equal(a.PalletId, d.PalletId);
        Assert.NotEqual(a.PalletId, e.PalletId);
        Assert.NotEqual(x1.PalletId, x2.PalletId);
    }
}
