using System.Globalization;
using LagerPalleSortering.Domain;
using Microsoft.Data.Sqlite;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class SqliteWarehouseRepository
{
    private static async Task InsertScanEntryAsync(SqliteConnection connection, SqliteTransaction tx, ScanEntryRecord entry, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO ScanEntries(Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt)
            VALUES($ts, $product, $expiry, $qty, $pallet, $key, $created, $confirmedQty, $confirmed, $confirmedAt);
            """;
        cmd.Parameters.AddWithValue("$ts", entry.Timestamp.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$product", entry.ProductNumber);
        cmd.Parameters.AddWithValue("$expiry", entry.ExpiryDate);
        cmd.Parameters.AddWithValue("$qty", entry.Quantity);
        cmd.Parameters.AddWithValue("$pallet", entry.PalletId);
        cmd.Parameters.AddWithValue("$key", entry.GroupKey);
        cmd.Parameters.AddWithValue("$created", entry.CreatedNewPallet ? 1 : 0);
        cmd.Parameters.AddWithValue("$confirmedQty", entry.ConfirmedQuantity);
        cmd.Parameters.AddWithValue("$confirmed", entry.ConfirmedMoved ? 1 : 0);
        cmd.Parameters.AddWithValue("$confirmedAt", entry.ConfirmedAt?.ToString("O", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<ScanEntryRecord?> GetLatestUnconfirmedEntryForPalletAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt
            FROM ScanEntries
            WHERE PalletId = $palletId AND ConfirmedQuantity < Quantity
            ORDER BY Id DESC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$palletId", palletId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadEntry(reader);
    }

    private static async Task MarkEntryConfirmedAsync(SqliteConnection connection, SqliteTransaction tx, long entryId, DateTime confirmedAt, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        // Confirm one physical colli per scan. Entry is fully confirmed when
        // ConfirmedQuantity reaches Quantity.
        cmd.CommandText = """
            UPDATE ScanEntries
            SET ConfirmedQuantity = CASE
                    WHEN ConfirmedQuantity < Quantity THEN ConfirmedQuantity + 1
                    ELSE ConfirmedQuantity
                END,
                ConfirmedMoved = CASE
                    WHEN (CASE WHEN ConfirmedQuantity < Quantity THEN ConfirmedQuantity + 1 ELSE ConfirmedQuantity END) >= Quantity THEN 1
                    ELSE 0
                END,
                ConfirmedAt = $confirmedAt
            WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$confirmedAt", confirmedAt.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$id", entryId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<ScanEntryRecord?> GetLastEntryAsync(SqliteConnection connection, SqliteTransaction tx, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt
            FROM ScanEntries
            ORDER BY Id DESC
            LIMIT 1;
            """;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadEntry(reader);
    }

    private static async Task DeleteEntryAsync(SqliteConnection connection, SqliteTransaction tx, long id, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM ScanEntries WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
