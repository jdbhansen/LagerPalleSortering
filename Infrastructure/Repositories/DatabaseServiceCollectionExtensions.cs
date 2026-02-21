using LagerPalleSortering.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LagerPalleSortering.Infrastructure.Repositories;

public static class DatabaseServiceCollectionExtensions
{
    public static IServiceCollection AddWarehouseStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(configuration);
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        if (IsPostgresProvider(databaseOptions.Provider))
        {
            services.AddSingleton<IWarehouseRepository, PostgresWarehouseRepository>();
            return services;
        }

        services.AddSingleton<IWarehouseDatabaseProvider, SqliteWarehouseDatabaseProvider>();
        services.AddSingleton<IWarehouseRepository, SqliteWarehouseRepository>();
        return services;
    }

    private static bool IsPostgresProvider(string? provider)
    {
        return string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase)
               || string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase);
    }
}
