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
    private readonly bool _disableAuth;
    private readonly bool _configureAuthUser;
    private readonly bool _clearDefaultConfiguration;
    private readonly string _environmentName;
    private readonly string _testUsername;
    private readonly string _testPassword;
    private readonly string _storageRoot = Path.Combine(
        Path.GetTempPath(),
        "LagerPalleSorteringApiTests",
        Guid.NewGuid().ToString("N"));

    public WarehouseApiWebApplicationFactory(
        bool disableAuth = true,
        string testUsername = "admin",
        string testPassword = "ChangeMe-Now!",
        bool configureAuthUser = true,
        bool clearDefaultConfiguration = false,
        string environmentName = "Development")
    {
        _disableAuth = disableAuth;
        _testUsername = testUsername;
        _testPassword = testPassword;
        _configureAuthUser = configureAuthUser;
        _clearDefaultConfiguration = clearDefaultConfiguration;
        _environmentName = environmentName;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(_storageRoot);

        builder.UseEnvironment(_environmentName);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            if (_clearDefaultConfiguration)
            {
                config.Sources.Clear();
            }

            var settings = new Dictionary<string, string?>
            {
                ["DisableHttpsRedirection"] = "true",
                ["Auth:RequireAuthentication"] = _disableAuth ? "false" : "true"
            };

            if (_configureAuthUser)
            {
                settings["Auth:Users:0:Username"] = _testUsername;
                settings["Auth:Users:0:Password"] = _testPassword;
            }

            config.AddInMemoryCollection(settings);
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
