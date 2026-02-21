using System.Globalization;
using LagerPalleSortering.Domain;
using Npgsql;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class PostgresWarehouseRepository
{
    private static async Task InsertScanEntryAsync(NpgsqlConnection connection, NpgsqlTransaction tx, ScanEntryRecord entry, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO ScanEntries(Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt)
            VALUES(@ts, @product, @expiry, @qty, @pallet, @key, @created, @confirmedQty, @confirmed, @confirmedAt);
            """;
        cmd.Parameters.AddWithValue("@ts", entry.Timestamp);
        cmd.Parameters.AddWithValue("@product", entry.ProductNumber);
        cmd.Parameters.AddWithValue("@expiry", entry.ExpiryDate);
        cmd.Parameters.AddWithValue("@qty", entry.Quantity);
        cmd.Parameters.AddWithValue("@pallet", entry.PalletId);
        cmd.Parameters.AddWithValue("@key", entry.GroupKey);
        cmd.Parameters.AddWithValue("@created", entry.CreatedNewPallet);
        cmd.Parameters.AddWithValue("@confirmedQty", entry.ConfirmedQuantity);
        cmd.Parameters.AddWithValue("@confirmed", entry.ConfirmedMoved);
        cmd.Parameters.AddWithValue("@confirmedAt", entry.ConfirmedAt ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<ScanEntryRecord?> GetLatestUnconfirmedEntryForPalletAsync(NpgsqlConnection connection, NpgsqlTransaction tx, string palletId, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt
            FROM ScanEntries
            WHERE PalletId = @palletId AND ConfirmedQuantity < Quantity
            -- Newest-first aligns with operator expectation: confirm latest placed colli first.
            ORDER BY Id DESC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("@palletId", palletId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadEntry(reader);
    }

    private static async Task MarkEntryConfirmedAsync(NpgsqlConnection connection, NpgsqlTransaction tx, long entryId, DateTime confirmedAt, CancellationToken cancellationToken)
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
                    WHEN (CASE WHEN ConfirmedQuantity < Quantity THEN ConfirmedQuantity + 1 ELSE ConfirmedQuantity END) >= Quantity THEN TRUE
                    ELSE FALSE
                END,
                ConfirmedAt = @confirmedAt
            WHERE Id = @id;
            """;
        cmd.Parameters.AddWithValue("@confirmedAt", confirmedAt);
        cmd.Parameters.AddWithValue("@id", entryId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<ScanEntryRecord?> GetLastEntryAsync(NpgsqlConnection connection, NpgsqlTransaction tx, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        // Undo is intentionally LIFO to match operator expectation.
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

    private static async Task DeleteEntryAsync(NpgsqlConnection connection, NpgsqlTransaction tx, long id, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM ScanEntries WHERE Id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

