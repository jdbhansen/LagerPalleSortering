using System.Globalization;
using LagerPalleSortering.Domain;
using Microsoft.Data.Sqlite;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class SqliteWarehouseRepository
{
    private async Task<PalletRecord?> FindBestOpenPalletAsync(SqliteConnection connection, SqliteTransaction tx, string productNumber, string expiryDate, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT
                p.PalletId,
                p.TotalQuantity,
                p.IsClosed,
                p.CreatedAt,
                COUNT(pi.Id) AS VariantCount,
                SUM(CASE WHEN pi.ProductNumber = $product AND pi.ExpiryDate = $expiry THEN 1 ELSE 0 END) AS HasExact,
                SUM(CASE WHEN pi.ProductNumber = $product AND pi.ExpiryDate <> $expiry THEN 1 ELSE 0 END) AS HasConflict
            FROM Pallets p
            LEFT JOIN PalletItems pi ON pi.PalletId = p.PalletId
            WHERE p.IsClosed = 0
            GROUP BY p.PalletId, p.TotalQuantity, p.IsClosed, p.CreatedAt
            -- Prefer exact variant match first, then stable numeric pallet order.
            ORDER BY HasExact DESC, CAST(SUBSTR(p.PalletId, 3) AS INTEGER), p.PalletId;
            """;
        cmd.Parameters.AddWithValue("$product", productNumber);
        cmd.Parameters.AddWithValue("$expiry", expiryDate);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var variantCount = reader.GetInt32(4);
            var hasExact = reader.GetInt64(5) > 0;
            var hasConflict = reader.GetInt64(6) > 0;

            // Never mix same product barcode with different expiry on one pallet.
            if (hasConflict)
            {
                continue;
            }

            // Enforce configurable max variants unless this is an existing exact variant.
            if (!hasExact && variantCount >= _maxVariantsPerPallet)
            {
                continue;
            }

            return new PalletRecord(
                reader.GetString(0),
                hasExact ? BuildKey(productNumber, expiryDate) : "MIXED",
                hasExact ? productNumber : "BLANDET",
                hasExact ? expiryDate : "MIX",
                reader.GetInt32(1),
                reader.GetInt32(2) == 1,
                DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
        }

        return null;
    }

    private static async Task<int> GetNextPalletNumberAsync(SqliteConnection connection, SqliteTransaction tx, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT COALESCE(MAX(CAST(SUBSTR(PalletId, 3) AS INTEGER)), 0)
            FROM Pallets;
            """;
        var current = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        return current + 1;
    }

    private static async Task InsertPalletAsync(SqliteConnection connection, SqliteTransaction tx, PalletRecord pallet, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO Pallets(PalletId, GroupKey, ProductNumber, ExpiryDate, TotalQuantity, IsClosed, CreatedAt)
            VALUES($id, $key, $product, $expiry, $qty, $closed, $created);
            """;
        cmd.Parameters.AddWithValue("$id", pallet.PalletId);
        cmd.Parameters.AddWithValue("$key", pallet.GroupKey);
        cmd.Parameters.AddWithValue("$product", pallet.ProductNumber);
        cmd.Parameters.AddWithValue("$expiry", pallet.ExpiryDate);
        cmd.Parameters.AddWithValue("$qty", pallet.TotalQuantity);
        cmd.Parameters.AddWithValue("$closed", pallet.IsClosed ? 1 : 0);
        cmd.Parameters.AddWithValue("$created", pallet.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpsertPalletItemAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, string productNumber, string expiryDate, int quantity, CancellationToken cancellationToken)
    {
        // Update-first keeps write path simple and avoids a read-before-write roundtrip.
        await using var update = connection.CreateCommand();
        update.Transaction = tx;
        update.CommandText = """
            UPDATE PalletItems
            SET Quantity = Quantity + $qty
            WHERE PalletId = $palletId AND ProductNumber = $product AND ExpiryDate = $expiry;
            """;
        update.Parameters.AddWithValue("$qty", quantity);
        update.Parameters.AddWithValue("$palletId", palletId);
        update.Parameters.AddWithValue("$product", productNumber);
        update.Parameters.AddWithValue("$expiry", expiryDate);
        var rows = await update.ExecuteNonQueryAsync(cancellationToken);

        if (rows > 0)
        {
            return;
        }

        await using var insert = connection.CreateCommand();
        insert.Transaction = tx;
        insert.CommandText = """
            INSERT INTO PalletItems(PalletId, ProductNumber, ExpiryDate, Quantity)
            VALUES($palletId, $product, $expiry, $qty);
            """;
        insert.Parameters.AddWithValue("$palletId", palletId);
        insert.Parameters.AddWithValue("$product", productNumber);
        insert.Parameters.AddWithValue("$expiry", expiryDate);
        insert.Parameters.AddWithValue("$qty", quantity);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DecreaseOrDeletePalletItemAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, string productNumber, string expiryDate, int quantity, CancellationToken cancellationToken)
    {
        await using var read = connection.CreateCommand();
        read.Transaction = tx;
        read.CommandText = """
            SELECT Quantity
            FROM PalletItems
            WHERE PalletId = $palletId AND ProductNumber = $product AND ExpiryDate = $expiry
            LIMIT 1;
            """;
        read.Parameters.AddWithValue("$palletId", palletId);
        read.Parameters.AddWithValue("$product", productNumber);
        read.Parameters.AddWithValue("$expiry", expiryDate);

        var currentObj = await read.ExecuteScalarAsync(cancellationToken);
        if (currentObj is null)
        {
            return;
        }

        var current = Convert.ToInt32(currentObj, CultureInfo.InvariantCulture);
        var updated = current - quantity;
        if (updated > 0)
        {
            await using var update = connection.CreateCommand();
            update.Transaction = tx;
            update.CommandText = """
                UPDATE PalletItems
                SET Quantity = $qty
                WHERE PalletId = $palletId AND ProductNumber = $product AND ExpiryDate = $expiry;
                """;
            update.Parameters.AddWithValue("$qty", updated);
            update.Parameters.AddWithValue("$palletId", palletId);
            update.Parameters.AddWithValue("$product", productNumber);
            update.Parameters.AddWithValue("$expiry", expiryDate);
            await update.ExecuteNonQueryAsync(cancellationToken);
            return;
        }

        await using var delete = connection.CreateCommand();
        delete.Transaction = tx;
        delete.CommandText = """
            DELETE FROM PalletItems
            WHERE PalletId = $palletId AND ProductNumber = $product AND ExpiryDate = $expiry;
            """;
        delete.Parameters.AddWithValue("$palletId", palletId);
        delete.Parameters.AddWithValue("$product", productNumber);
        delete.Parameters.AddWithValue("$expiry", expiryDate);
        await delete.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task UpdatePalletQuantityAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, int quantity, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE Pallets SET TotalQuantity = $qty WHERE PalletId = $id;";
        cmd.Parameters.AddWithValue("$qty", quantity);
        cmd.Parameters.AddWithValue("$id", palletId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<PalletRecord?> GetPalletByIdAsync(SqliteConnection connection, SqliteTransaction? tx, string palletId, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT
                p.PalletId,
                p.TotalQuantity,
                p.IsClosed,
                p.CreatedAt,
                COUNT(pi.Id) AS VariantCount,
                MIN(pi.ProductNumber) AS SingleProduct,
                MIN(pi.ExpiryDate) AS SingleExpiry
            FROM Pallets p
            LEFT JOIN PalletItems pi ON pi.PalletId = p.PalletId
            WHERE p.PalletId = $id
            GROUP BY p.PalletId, p.TotalQuantity, p.IsClosed, p.CreatedAt
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$id", palletId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return ReadPalletSummary(reader);
    }

    private static async Task DeletePalletItemsAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM PalletItems WHERE PalletId = $id;";
        cmd.Parameters.AddWithValue("$id", palletId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeletePalletAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM Pallets WHERE PalletId = $id;";
        cmd.Parameters.AddWithValue("$id", palletId);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
