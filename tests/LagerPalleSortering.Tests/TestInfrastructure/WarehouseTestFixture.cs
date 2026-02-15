using LagerPalleSortering.Application.Services;
using LagerPalleSortering.Infrastructure.Repositories;

namespace LagerPalleSortering.Tests.TestInfrastructure;

internal sealed class WarehouseTestFixture : IDisposable, IAsyncDisposable
{
    private readonly string rootPath;

    private WarehouseTestFixture(string rootPath, WarehouseDataService service, WarehouseExportService exportService)
    {
        this.rootPath = rootPath;
        Service = service;
        ExportService = exportService;
    }

    public WarehouseDataService Service { get; }
    public WarehouseExportService ExportService { get; }

    public static async Task<WarehouseTestFixture> CreateAsync(string scenarioPrefix)
    {
        var root = Path.Combine(Path.GetTempPath(), scenarioPrefix, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var repository = new SqliteWarehouseRepository(new TestWebHostEnvironment(root));
        var service = new WarehouseDataService(repository);
        var exportService = new WarehouseExportService(repository);
        await service.InitializeAsync();

        return new WarehouseTestFixture(root, service, exportService);
    }

    public async Task<WarehouseDataService> CreateNewServiceForSameStorageAsync()
    {
        var repository = new SqliteWarehouseRepository(new TestWebHostEnvironment(rootPath));
        var service = new WarehouseDataService(repository);
        await service.InitializeAsync();
        return service;
    }

    public void Dispose()
    {
        TryDeleteRoot();
    }

    public ValueTask DisposeAsync()
    {
        TryDeleteRoot();
        return ValueTask.CompletedTask;
    }

    private void TryDeleteRoot()
    {
        try
        {
            Directory.Delete(rootPath, recursive: true);
        }
        catch
        {
        }
    }
}
