using Microsoft.Data.Sqlite;

namespace LagerPalleSortering.Infrastructure.Repositories;

/// <summary>
/// Default file-based SQLite provider. Swappable in DI when migrating storage backend.
/// </summary>
public sealed class SqliteWarehouseDatabaseProvider : IWarehouseDatabaseProvider
{
    private readonly string _connectionString;

    public SqliteWarehouseDatabaseProvider(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        DatabasePath = Path.Combine(dataDir, "lager.db");
        _connectionString = $"Data Source={DatabasePath}";
    }

    public string DatabasePath { get; }

    public SqliteConnection CreateConnection() => new(_connectionString);
}
