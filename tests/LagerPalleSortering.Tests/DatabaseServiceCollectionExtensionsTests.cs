using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LagerPalleSortering.Tests;

public sealed class DatabaseServiceCollectionExtensionsTests
{
    [Fact]
    public void AddWarehouseStorage_WithPostgresProvider_RegistersPostgresRepository()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("Postgres");

        services.AddWarehouseStorage(configuration);

        var descriptor = services.Single(x => x.ServiceType == typeof(IWarehouseRepository));
        Assert.Equal(typeof(PostgresWarehouseRepository), descriptor.ImplementationType);
    }

    [Fact]
    public void AddWarehouseStorage_WithSqliteProvider_RegistersSqliteRepository()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("Sqlite");

        services.AddWarehouseStorage(configuration);

        var descriptor = services.Single(x => x.ServiceType == typeof(IWarehouseRepository));
        Assert.Equal(typeof(SqliteWarehouseRepository), descriptor.ImplementationType);
    }

    private static IConfiguration BuildConfiguration(string provider)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Database:Provider", provider),
                new KeyValuePair<string, string?>("Database:ConnectionString", "Host=localhost;Port=5432;Database=test;Username=test;Password=test")
            ])
            .Build();
    }
}

