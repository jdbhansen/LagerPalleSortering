using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class SqliteWarehouseRepository : IWarehouseRepository, IDisposable
{
    private readonly int _maxVariantsPerPallet;
    private readonly string _databasePath;
    private readonly string _connectionString;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public SqliteWarehouseRepository(IWebHostEnvironment env, IOptions<WarehouseRulesOptions>? rules = null)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDir);
        _databasePath = Path.Combine(dataDir, "lager.db");
        _connectionString = $"Data Source={_databasePath}";
        _maxVariantsPerPallet = Math.Max(1, rules?.Value.MaxVariantsPerPallet ?? 4);
    }

    public async Task<(string PalletId, bool CreatedNewPallet)> RegisterAsync(string productNumber, string expiryDate, int quantity, CancellationToken cancellationToken = default)
    {
        // Single-writer lock protects multi-step updates across Pallets/PalletItems/ScanEntries.
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            using var tx = connection.BeginTransaction();

            var groupKey = BuildKey(productNumber, expiryDate);
            var pallet = await FindBestOpenPalletAsync(connection, tx, productNumber, expiryDate, cancellationToken);
            var createdNew = false;

            if (pallet is null)
            {
                var nextNumber = await GetNextPalletNumberAsync(connection, tx, cancellationToken);
                pallet = new PalletRecord(
                    $"P-{nextNumber:000}",
                    groupKey,
                    productNumber,
                    expiryDate,
                    0,
                    false,
                    DateTime.UtcNow);
                await InsertPalletAsync(connection, tx, pallet, cancellationToken);
                createdNew = true;
            }

            await UpsertPalletItemAsync(connection, tx, pallet.PalletId, productNumber, expiryDate, quantity, cancellationToken);
            await UpdatePalletQuantityAsync(connection, tx, pallet.PalletId, pallet.TotalQuantity + quantity, cancellationToken);

            var entry = new ScanEntryRecord(
                0,
                DateTime.UtcNow,
                productNumber,
                expiryDate,
                quantity,
                pallet.PalletId,
                groupKey,
                createdNew,
                0,
                false,
                null);
            await InsertScanEntryAsync(connection, tx, entry, cancellationToken);
            await InsertAuditEntryAsync(connection, tx, "REGISTER_COLLI", $"Pallet={pallet.PalletId};Qty={quantity};Product={productNumber};Expiry={expiryDate}", cancellationToken);

            await tx.CommitAsync(cancellationToken);
            return (pallet.PalletId, createdNew);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            using var tx = connection.BeginTransaction();
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "UPDATE Pallets SET IsClosed = 1 WHERE PalletId = $id;";
            cmd.Parameters.AddWithValue("$id", palletId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
            await InsertAuditEntryAsync(connection, tx, "CLOSE_PALLET", $"Pallet={palletId}", cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<long?> ConfirmLatestUnconfirmedByPalletIdAsync(string palletId, DateTime confirmedAtUtc, CancellationToken cancellationToken = default)
    {
        // Confirmation updates are serialized to prevent double-increment races.
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            using var tx = connection.BeginTransaction();

            var entry = await GetLatestUnconfirmedEntryForPalletAsync(connection, tx, palletId, cancellationToken);
            if (entry is null)
            {
                return null;
            }

            await MarkEntryConfirmedAsync(connection, tx, entry.Id, confirmedAtUtc, cancellationToken);
            await InsertAuditEntryAsync(connection, tx, "CONFIRM_MOVE", $"Pallet={palletId};ScanEntryId={entry.Id}", cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return entry.Id;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default)
    {
        // Undo operates on latest entry only to keep behavior deterministic for operators.
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            using var tx = connection.BeginTransaction();

            var entry = await GetLastEntryAsync(connection, tx, cancellationToken);
            if (entry is null)
            {
                return null;
            }

            await DeleteEntryAsync(connection, tx, entry.Id, cancellationToken);
            await DecreaseOrDeletePalletItemAsync(connection, tx, entry.PalletId, entry.ProductNumber, entry.ExpiryDate, entry.Quantity, cancellationToken);

            var pallet = await GetPalletByIdAsync(connection, tx, entry.PalletId, cancellationToken);
            if (pallet is not null)
            {
                var updated = pallet.TotalQuantity - entry.Quantity;
                if (updated <= 0 && entry.CreatedNewPallet)
                {
                    // Remove pallet shell when undoing the very first registration for that pallet.
                    await DeletePalletItemsAsync(connection, tx, pallet.PalletId, cancellationToken);
                    await DeletePalletAsync(connection, tx, pallet.PalletId, cancellationToken);
                }
                else
                {
                    await UpdatePalletQuantityAsync(connection, tx, pallet.PalletId, Math.Max(0, updated), cancellationToken);
                }
            }

            await InsertAuditEntryAsync(connection, tx, "UNDO_LAST", $"Pallet={entry.PalletId};Qty={entry.Quantity}", cancellationToken);
            await tx.CommitAsync(cancellationToken);
            return new UndoResult(entry.PalletId, entry.Quantity);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            using var tx = connection.BeginTransaction();

            await using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = """
                    DELETE FROM ScanEntries;
                    DELETE FROM AuditEntries;
                    DELETE FROM PalletItems;
                    DELETE FROM Pallets;
                    DELETE FROM sqlite_sequence WHERE name IN ('ScanEntries', 'PalletItems', 'AuditEntries');
                    """;
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await InsertAuditEntryAsync(connection, tx, "CLEAR_ALL_DATA", "All operational rows deleted", cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<List<PalletRecord>> GetOpenPalletsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
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
            ORDER BY CAST(SUBSTR(p.PalletId, 3) AS INTEGER), p.PalletId;
            """;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var list = new List<PalletRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(ReadPalletSummary(reader));
        }

        return list;
    }

    public async Task<List<ScanEntryRecord>> GetRecentEntriesAsync(int maxEntries, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt
            FROM ScanEntries
            ORDER BY Id DESC
            LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$limit", maxEntries);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var list = new List<ScanEntryRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(ReadEntry(reader));
        }

        return list;
    }

    public async Task<List<AuditEntryRecord>> GetRecentAuditEntriesAsync(int maxEntries, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Timestamp, Action, Details, MachineName
            FROM AuditEntries
            ORDER BY Id DESC
            LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$limit", maxEntries);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var list = new List<AuditEntryRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(ReadAuditEntry(reader));
        }

        return list;
    }

    public async Task<WarehouseHealthSnapshot> GetHealthSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT
                (SELECT COUNT(1) FROM Pallets WHERE IsClosed = 0) AS OpenPallets,
                (SELECT COALESCE(SUM(TotalQuantity), 0) FROM Pallets WHERE IsClosed = 0) AS OpenColli,
                (SELECT COALESCE(SUM(Quantity - ConfirmedQuantity), 0) FROM ScanEntries WHERE ConfirmedQuantity < Quantity) AS PendingConfirmations,
                (SELECT Timestamp FROM ScanEntries ORDER BY Id DESC LIMIT 1) AS LastEntryTimestampUtc;
            """;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new WarehouseHealthSnapshot(0, 0, 0, null);
        }

        return new WarehouseHealthSnapshot(
            Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
            Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture),
            reader.IsDBNull(3)
                ? null
                : DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    public async Task<byte[]> BackupDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_databasePath))
            {
                return Array.Empty<byte>();
            }

            var backupPath = Path.Combine(Path.GetDirectoryName(_databasePath)!, $"backup-{Guid.NewGuid():N}.db");
            try
            {
                await using (var connection = OpenConnection())
                {
                    await connection.OpenAsync(cancellationToken);
                    await using var backupCmd = connection.CreateCommand();
                    // Create a consistent sqlite snapshot even if WAL is enabled.
                    backupCmd.CommandText = $"VACUUM INTO '{backupPath.Replace("'", "''", StringComparison.Ordinal)}';";
                    await backupCmd.ExecuteNonQueryAsync(cancellationToken);
                }

                var bytes = await File.ReadAllBytesAsync(backupPath, cancellationToken);
                await using var auditConnection = OpenConnection();
                await auditConnection.OpenAsync(cancellationToken);
                using var tx = auditConnection.BeginTransaction();
                await InsertAuditEntryAsync(auditConnection, tx, "BACKUP_DATABASE", $"Bytes={bytes.Length}", cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return bytes;
            }
            finally
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task RestoreDatabaseAsync(Stream databaseStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseStream);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var tempPath = Path.Combine(Path.GetDirectoryName(_databasePath)!, $"restore-{Guid.NewGuid():N}.db");
            try
            {
                await using (var file = File.Create(tempPath))
                {
                    await databaseStream.CopyToAsync(file, cancellationToken);
                }

                // Validate sqlite integrity before swap.
                await using (var validate = new SqliteConnection($"Data Source={tempPath}"))
                {
                    await validate.OpenAsync(cancellationToken);
                    await using var pragma = validate.CreateCommand();
                    pragma.CommandText = "PRAGMA quick_check;";
                    var result = Convert.ToString(await pragma.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
                    if (!string.Equals(result, "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("Backupfilen fejlede SQLite quick_check.");
                    }
                }

                File.Copy(tempPath, _databasePath, overwrite: true);
                await InitializeAsync(cancellationToken);

                await using var connection = OpenConnection();
                await connection.OpenAsync(cancellationToken);
                using var tx = connection.BeginTransaction();
                await InsertAuditEntryAsync(connection, tx, "RESTORE_DATABASE", "Database restored from uploaded backup", cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch
                    {
                        // Best-effort cleanup only; restore succeeded at this point.
                    }
                }
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<List<PalletContentItemRecord>> GetPalletContentsAsync(string palletId, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT ProductNumber, ExpiryDate, Quantity
            FROM PalletItems
            WHERE PalletId = $id
            ORDER BY ProductNumber, ExpiryDate;
            """;
        cmd.Parameters.AddWithValue("$id", palletId);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        var list = new List<PalletContentItemRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PalletContentItemRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetInt32(2)));
        }

        return list;
    }

    public async Task<PalletRecord?> GetPalletByIdAsync(string palletId, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetPalletByIdAsync(connection, null, palletId, cancellationToken);
    }

    public void Dispose()
    {
        _writeLock.Dispose();
    }
}
