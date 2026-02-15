using System.Globalization;
using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;
using Microsoft.Data.Sqlite;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed class SqliteWarehouseRepository : IWarehouseRepository, IDisposable
{
    private const int MaxVariantsPerPallet = 4;

    private readonly string connectionString;
    private readonly SemaphoreSlim writeLock = new(1, 1);

    public SqliteWarehouseRepository(IWebHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        connectionString = $"Data Source={Path.Combine(dataDir, "lager.db")}";
    }

    public async Task InitializeAsync()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();

        var sql = """
            CREATE TABLE IF NOT EXISTS Pallets (
                PalletId TEXT PRIMARY KEY,
                GroupKey TEXT NOT NULL,
                ProductNumber TEXT NOT NULL,
                ExpiryDate TEXT NOT NULL,
                TotalQuantity INTEGER NOT NULL,
                IsClosed INTEGER NOT NULL,
                CreatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS PalletItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PalletId TEXT NOT NULL,
                ProductNumber TEXT NOT NULL,
                ExpiryDate TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UNIQUE(PalletId, ProductNumber, ExpiryDate)
            );

            CREATE TABLE IF NOT EXISTS ScanEntries (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                ProductNumber TEXT NOT NULL,
                ExpiryDate TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                PalletId TEXT NOT NULL,
                GroupKey TEXT NOT NULL,
                CreatedNewPallet INTEGER NOT NULL,
                ConfirmedMoved INTEGER NOT NULL DEFAULT 0,
                ConfirmedAt TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Pallets_GroupKey_Open ON Pallets(GroupKey, IsClosed);
            CREATE INDEX IF NOT EXISTS IX_PalletItems_PalletId ON PalletItems(PalletId);
            CREATE INDEX IF NOT EXISTS IX_ScanEntries_Id ON ScanEntries(Id DESC);
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();

        await EnsureColumnExistsAsync(connection, "ScanEntries", "ConfirmedMoved", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnExistsAsync(connection, "ScanEntries", "ConfirmedAt", "TEXT NULL");
        await MigrateLegacyPalletRowsToPalletItemsAsync(connection);
    }

    public async Task<(string PalletId, bool CreatedNewPallet)> RegisterAsync(string productNumber, string expiryDate, int quantity)
    {
        await writeLock.WaitAsync();
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync();
            using var tx = connection.BeginTransaction();

            var groupKey = BuildKey(productNumber, expiryDate);
            var pallet = await FindBestOpenPalletAsync(connection, tx, productNumber, expiryDate);
            var createdNew = false;

            if (pallet is null)
            {
                var nextNumber = await GetNextPalletNumberAsync(connection, tx);
                pallet = new PalletRecord(
                    $"P-{nextNumber:000}",
                    groupKey,
                    productNumber,
                    expiryDate,
                    0,
                    false,
                    DateTime.UtcNow);
                await InsertPalletAsync(connection, tx, pallet);
                createdNew = true;
            }

            await UpsertPalletItemAsync(connection, tx, pallet.PalletId, productNumber, expiryDate, quantity);
            await UpdatePalletQuantityAsync(connection, tx, pallet.PalletId, pallet.TotalQuantity + quantity);

            var entry = new ScanEntryRecord(
                0,
                DateTime.UtcNow,
                productNumber,
                expiryDate,
                quantity,
                pallet.PalletId,
                groupKey,
                createdNew,
                false,
                null);
            await InsertScanEntryAsync(connection, tx, entry);

            await tx.CommitAsync();
            return (pallet.PalletId, createdNew);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task ClosePalletAsync(string palletId)
    {
        await writeLock.WaitAsync();
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync();
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Pallets SET IsClosed = 1 WHERE PalletId = $id;";
            cmd.Parameters.AddWithValue("$id", palletId);
            await cmd.ExecuteNonQueryAsync();
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<long?> ConfirmLatestUnconfirmedByPalletIdAsync(string palletId, DateTime confirmedAtUtc)
    {
        await writeLock.WaitAsync();
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync();
            using var tx = connection.BeginTransaction();

            var entry = await GetLatestUnconfirmedEntryForPalletAsync(connection, tx, palletId);
            if (entry is null)
            {
                return null;
            }

            await MarkEntryConfirmedAsync(connection, tx, entry.Id, confirmedAtUtc);
            await tx.CommitAsync();
            return entry.Id;
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<UndoResult?> UndoLastAsync()
    {
        await writeLock.WaitAsync();
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync();
            using var tx = connection.BeginTransaction();

            var entry = await GetLastEntryAsync(connection, tx);
            if (entry is null)
            {
                return null;
            }

            await DeleteEntryAsync(connection, tx, entry.Id);
            await DecreaseOrDeletePalletItemAsync(connection, tx, entry.PalletId, entry.ProductNumber, entry.ExpiryDate, entry.Quantity);

            var pallet = await GetPalletByIdAsync(connection, tx, entry.PalletId);
            if (pallet is not null)
            {
                var updated = pallet.TotalQuantity - entry.Quantity;
                if (updated <= 0 && entry.CreatedNewPallet)
                {
                    await DeletePalletItemsAsync(connection, tx, pallet.PalletId);
                    await DeletePalletAsync(connection, tx, pallet.PalletId);
                }
                else
                {
                    await UpdatePalletQuantityAsync(connection, tx, pallet.PalletId, Math.Max(0, updated));
                }
            }

            await tx.CommitAsync();
            return new UndoResult(entry.PalletId, entry.Quantity);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<List<PalletRecord>> GetOpenPalletsAsync()
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
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
            WHERE p.IsClosed = 0
            GROUP BY p.PalletId, p.TotalQuantity, p.IsClosed, p.CreatedAt
            ORDER BY p.PalletId;
            """;
        await using var reader = await cmd.ExecuteReaderAsync();

        var list = new List<PalletRecord>();
        while (await reader.ReadAsync())
        {
            list.Add(ReadPalletSummary(reader));
        }

        return list;
    }

    public async Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedMoved, ConfirmedAt
            FROM ScanEntries
            ORDER BY Id DESC
            LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$limit", maxEntries);
        await using var reader = await cmd.ExecuteReaderAsync();

        var list = new List<ScanEntryRecord>();
        while (await reader.ReadAsync())
        {
            list.Add(ReadEntry(reader));
        }

        return list;
    }

    public async Task<PalletRecord?> GetPalletByIdAsync(string palletId)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync();
        return await GetPalletByIdAsync(connection, null, palletId);
    }

    private SqliteConnection OpenConnection() => new(connectionString);

    private static string BuildKey(string product, string expiry) => $"{product}|{expiry}";

    private static async Task EnsureColumnExistsAsync(SqliteConnection connection, string table, string column, string definitionSql)
    {
        await using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table});";
        await using var reader = await check.ExecuteReaderAsync();

        var found = false;
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                break;
            }
        }

        if (found)
        {
            return;
        }

        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definitionSql};";
        await alter.ExecuteNonQueryAsync();
    }

    private static async Task MigrateLegacyPalletRowsToPalletItemsAsync(SqliteConnection connection)
    {
        await using var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(1) FROM PalletItems;";
        var itemCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        if (itemCount > 0)
        {
            return;
        }

        await using var migrateCmd = connection.CreateCommand();
        migrateCmd.CommandText = """
            INSERT INTO PalletItems(PalletId, ProductNumber, ExpiryDate, Quantity)
            SELECT PalletId, ProductNumber, ExpiryDate, TotalQuantity
            FROM Pallets
            WHERE ProductNumber IS NOT NULL AND ProductNumber <> '';
            """;
        await migrateCmd.ExecuteNonQueryAsync();
    }

    private static async Task<PalletRecord?> FindBestOpenPalletAsync(SqliteConnection connection, SqliteTransaction tx, string productNumber, string expiryDate)
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
            ORDER BY HasExact DESC, p.PalletId;
            """;
        cmd.Parameters.AddWithValue("$product", productNumber);
        cmd.Parameters.AddWithValue("$expiry", expiryDate);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var variantCount = reader.GetInt32(4);
            var hasExact = reader.GetInt64(5) > 0;
            var hasConflict = reader.GetInt64(6) > 0;

            if (hasConflict)
            {
                continue;
            }

            if (!hasExact && variantCount >= MaxVariantsPerPallet)
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

    private static async Task<int> GetNextPalletNumberAsync(SqliteConnection connection, SqliteTransaction tx)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT COALESCE(MAX(CAST(SUBSTR(PalletId, 3) AS INTEGER)), 0)
            FROM Pallets;
            """;
        var current = Convert.ToInt32(await cmd.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        return current + 1;
    }

    private static async Task InsertPalletAsync(SqliteConnection connection, SqliteTransaction tx, PalletRecord pallet)
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
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task UpsertPalletItemAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, string productNumber, string expiryDate, int quantity)
    {
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
        var rows = await update.ExecuteNonQueryAsync();

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
        await insert.ExecuteNonQueryAsync();
    }

    private static async Task DecreaseOrDeletePalletItemAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, string productNumber, string expiryDate, int quantity)
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

        var currentObj = await read.ExecuteScalarAsync();
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
            await update.ExecuteNonQueryAsync();
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
        await delete.ExecuteNonQueryAsync();
    }

    private static async Task UpdatePalletQuantityAsync(SqliteConnection connection, SqliteTransaction tx, string palletId, int quantity)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE Pallets SET TotalQuantity = $qty WHERE PalletId = $id;";
        cmd.Parameters.AddWithValue("$qty", quantity);
        cmd.Parameters.AddWithValue("$id", palletId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task InsertScanEntryAsync(SqliteConnection connection, SqliteTransaction tx, ScanEntryRecord entry)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO ScanEntries(Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedMoved, ConfirmedAt)
            VALUES($ts, $product, $expiry, $qty, $pallet, $key, $created, $confirmed, $confirmedAt);
            """;
        cmd.Parameters.AddWithValue("$ts", entry.Timestamp.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$product", entry.ProductNumber);
        cmd.Parameters.AddWithValue("$expiry", entry.ExpiryDate);
        cmd.Parameters.AddWithValue("$qty", entry.Quantity);
        cmd.Parameters.AddWithValue("$pallet", entry.PalletId);
        cmd.Parameters.AddWithValue("$key", entry.GroupKey);
        cmd.Parameters.AddWithValue("$created", entry.CreatedNewPallet ? 1 : 0);
        cmd.Parameters.AddWithValue("$confirmed", entry.ConfirmedMoved ? 1 : 0);
        cmd.Parameters.AddWithValue("$confirmedAt", entry.ConfirmedAt?.ToString("O", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<ScanEntryRecord?> GetLatestUnconfirmedEntryForPalletAsync(SqliteConnection connection, SqliteTransaction tx, string palletId)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedMoved, ConfirmedAt
            FROM ScanEntries
            WHERE PalletId = $palletId AND ConfirmedMoved = 0
            ORDER BY Id DESC
            LIMIT 1;
            """;
        cmd.Parameters.AddWithValue("$palletId", palletId);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return ReadEntry(reader);
    }

    private static async Task MarkEntryConfirmedAsync(SqliteConnection connection, SqliteTransaction tx, long entryId, DateTime confirmedAt)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            UPDATE ScanEntries
            SET ConfirmedMoved = 1,
                ConfirmedAt = $confirmedAt
            WHERE Id = $id;
            """;
        cmd.Parameters.AddWithValue("$confirmedAt", confirmedAt.ToString("O", CultureInfo.InvariantCulture));
        cmd.Parameters.AddWithValue("$id", entryId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<ScanEntryRecord?> GetLastEntryAsync(SqliteConnection connection, SqliteTransaction tx)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedMoved, ConfirmedAt
            FROM ScanEntries
            ORDER BY Id DESC
            LIMIT 1;
            """;
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return ReadEntry(reader);
    }

    private static async Task DeleteEntryAsync(SqliteConnection connection, SqliteTransaction tx, long id)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM ScanEntries WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<PalletRecord?> GetPalletByIdAsync(SqliteConnection connection, SqliteTransaction? tx, string palletId)
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
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return ReadPalletSummary(reader);
    }

    private static async Task DeletePalletItemsAsync(SqliteConnection connection, SqliteTransaction tx, string palletId)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM PalletItems WHERE PalletId = $id;";
        cmd.Parameters.AddWithValue("$id", palletId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task DeletePalletAsync(SqliteConnection connection, SqliteTransaction tx, string palletId)
    {
        await using var cmd = connection.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM Pallets WHERE PalletId = $id;";
        cmd.Parameters.AddWithValue("$id", palletId);
        await cmd.ExecuteNonQueryAsync();
    }

    private static PalletRecord ReadPalletSummary(SqliteDataReader reader)
    {
        var variantCount = reader.GetInt32(4);
        var product = variantCount == 1 && !reader.IsDBNull(5) ? reader.GetString(5) : $"BLANDET ({variantCount})";
        var expiry = variantCount == 1 && !reader.IsDBNull(6) ? reader.GetString(6) : "MIX";
        var key = variantCount == 1 ? BuildKey(product, expiry) : "MIXED";

        return new PalletRecord(
            reader.GetString(0),
            key,
            product,
            expiry,
            reader.GetInt32(1),
            reader.GetInt32(2) == 1,
            DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    private static ScanEntryRecord ReadEntry(SqliteDataReader reader) =>
        new(
            reader.GetInt64(0),
            DateTime.Parse(reader.GetString(1), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetInt32(4),
            reader.GetString(5),
            reader.GetString(6),
            reader.GetInt32(7) == 1,
            reader.GetInt32(8) == 1,
            reader.IsDBNull(9)
                ? null
                : DateTime.Parse(reader.GetString(9), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));

    public void Dispose()
    {
        writeLock.Dispose();
    }
}
