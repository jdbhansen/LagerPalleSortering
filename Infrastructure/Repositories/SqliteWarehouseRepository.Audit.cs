using System.Globalization;
using LagerPalleSortering.Domain;
using Microsoft.Data.Sqlite;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class SqliteWarehouseRepository
{
    private static async Task InsertAuditEntryAsync(
        SqliteConnection connection,
        SqliteTransaction tx,
        string action,
        string details,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO AuditEntries(Timestamp, Action, Details, MachineName)
            VALUES($ts, $action, $details, $machine);
            """;
        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$action", action);
        cmd.Parameters.AddWithValue("$details", details);
        cmd.Parameters.AddWithValue("$machine", Environment.MachineName);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static AuditEntryRecord ReadAuditEntry(SqliteDataReader reader) =>
        new(
            reader.GetInt64(0),
            DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4));
}
