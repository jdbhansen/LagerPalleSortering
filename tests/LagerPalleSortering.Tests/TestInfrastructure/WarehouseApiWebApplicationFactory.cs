using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LagerPalleSortering.Tests.TestInfrastructure;

internal sealed class WarehouseApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _storageRoot = Path.Combine(
        Path.GetTempPath(),
        "LagerPalleSorteringApiTests",
        Guid.NewGuid().ToString("N"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_storageRoot);

        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DisableHttpsRedirection"] = "true"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IWarehouseRepository>();
            services.AddSingleton<IWarehouseRepository>(_ => new SqliteWarehouseRepository(new SqliteWarehouseDatabaseProvider(new TestWebHostEnvironment(_storageRoot))));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        TryDeleteStorageRoot();
    }

    private void TryDeleteStorageRoot()
    {
        try
        {
            if (Directory.Exists(_storageRoot))
            {
                Directory.Delete(_storageRoot, recursive: true);
            }
        }
        catch
        {
        }
    }
}
