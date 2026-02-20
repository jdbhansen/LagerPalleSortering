using Microsoft.Data.Sqlite;

namespace LagerPalleSortering.Infrastructure.Repositories;

/// <summary>
/// Infrastructure seam for database connection and physical storage location.
/// </summary>
public interface IWarehouseDatabaseProvider
{
    string DatabasePath { get; }
    SqliteConnection CreateConnection();
}
