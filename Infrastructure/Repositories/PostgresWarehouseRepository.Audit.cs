using LagerPalleSortering.Domain;
using Npgsql;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class PostgresWarehouseRepository
{
    private static async Task InsertAuditEntryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction tx,
        string action,
        string details,
        CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        // Audit is written in the same transaction as domain changes for traceability.
        cmd.CommandText = """
            INSERT INTO AuditEntries(Timestamp, Action, Details, MachineName)
            VALUES(@ts, @action, @details, @machine);
            """;
        cmd.Parameters.AddWithValue("@ts", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@action", action);
        cmd.Parameters.AddWithValue("@details", details);
        cmd.Parameters.AddWithValue("@machine", Environment.MachineName);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static AuditEntryRecord ReadAuditEntry(NpgsqlDataReader reader) =>
        new(
            reader.GetInt64(0),
            reader.GetDateTime(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4));
}

