using System.Text;
using LagerPalleSortering.Application.Services;
using LagerPalleSortering.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace LagerPalleSortering.Tests;

public sealed class SanityTests
{
    [Fact]
    [Trait("Category", "Sanity")]
    public async Task EmptyDatabase_HasNoOpenPallets()
    {
        await using var fixture = await SanityFixture.CreateAsync();
        var pallets = await fixture.Service.GetOpenPalletsAsync();
        Assert.Empty(pallets);
    }

    [Fact]
    [Trait("Category", "Sanity")]
    public async Task HappyPath_RegisterConfirmAndExportCsv_Works()
    {
        await using var fixture = await SanityFixture.CreateAsync();

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
        await using var fixture = await SanityFixture.CreateAsync();

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

    private sealed class SanityFixture : IAsyncDisposable
    {
        private readonly string rootPath;
        public WarehouseDataService Service { get; }
        public WarehouseExportService ExportService { get; }

        private SanityFixture(string rootPath, WarehouseDataService service, WarehouseExportService exportService)
        {
            this.rootPath = rootPath;
            Service = service;
            ExportService = exportService;
        }

        public static async Task<SanityFixture> CreateAsync()
        {
            var root = Path.Combine(Path.GetTempPath(), "LagerPalleSorteringSanity", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var repository = new SqliteWarehouseRepository(new TestWebHostEnvironment(root));
            var service = new WarehouseDataService(repository);
            var exportService = new WarehouseExportService(repository);
            await service.InitializeAsync();
            return new SanityFixture(root, service, exportService);
        }

        public ValueTask DisposeAsync()
        {
            try
            {
                Directory.Delete(rootPath, recursive: true);
            }
            catch
            {
            }

            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string rootPath)
        {
            ApplicationName = "LagerPalleSortering.Tests";
            EnvironmentName = "Development";
            ContentRootPath = rootPath;
            WebRootPath = rootPath;
            ContentRootFileProvider = new NullFileProvider();
            WebRootFileProvider = new NullFileProvider();
        }

        public string ApplicationName { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
