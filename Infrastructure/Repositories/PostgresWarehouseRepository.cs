using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;
using Npgsql;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class PostgresWarehouseRepository : IWarehouseRepository, IDisposable
{
    private readonly int _maxVariantsPerPallet;
    private readonly string _connectionString;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public PostgresWarehouseRepository(
        IOptions<DatabaseOptions> databaseOptions,
        IConfiguration configuration,
        IOptions<WarehouseRulesOptions>? rules = null)
    {
        _maxVariantsPerPallet = Math.Max(1, rules?.Value.MaxVariantsPerPallet ?? 4);
        _connectionString =
            databaseOptions.Value.ConnectionString
            ?? configuration.GetConnectionString("Warehouse")
            ?? throw new InvalidOperationException(
                "Database.ConnectionString (eller ConnectionStrings:Warehouse) mangler for PostgreSQL.");
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
            cmd.CommandText = "UPDATE Pallets SET IsClosed = TRUE WHERE PalletId = @id;";
            cmd.Parameters.AddWithValue("@id", palletId);
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
                    TRUNCATE TABLE ScanEntries, AuditEntries, PalletItems, Pallets RESTART IDENTITY;
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
            WHERE p.IsClosed = FALSE
            GROUP BY p.PalletId, p.TotalQuantity, p.IsClosed, p.CreatedAt
            ORDER BY CAST(SUBSTRING(p.PalletId FROM 3) AS INTEGER), p.PalletId;
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
            LIMIT @limit;
            """;
        cmd.Parameters.AddWithValue("@limit", maxEntries);
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
            LIMIT @limit;
            """;
        cmd.Parameters.AddWithValue("@limit", maxEntries);
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
                (SELECT COUNT(1) FROM Pallets WHERE IsClosed = FALSE) AS OpenPallets,
                (SELECT COALESCE(SUM(TotalQuantity), 0) FROM Pallets WHERE IsClosed = FALSE) AS OpenColli,
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
                : reader.GetDateTime(3));
    }

    public async Task<byte[]> BackupDatabaseAsync(CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);

            var snapshot = new PostgresBackupSnapshot(
                await ReadPalletRowsAsync(connection, cancellationToken),
                await ReadPalletItemRowsAsync(connection, cancellationToken),
                await ReadScanEntryRowsAsync(connection, cancellationToken),
                await ReadAuditRowsAsync(connection, cancellationToken));

            var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot);

            using var tx = connection.BeginTransaction();
            await InsertAuditEntryAsync(connection, tx, "BACKUP_DATABASE", $"Bytes={bytes.Length};Format=json", cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return bytes;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task RestoreDatabaseAsync(Stream databaseStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(databaseStream);

        PostgresBackupSnapshot snapshot;
        try
        {
            snapshot = await JsonSerializer.DeserializeAsync<PostgresBackupSnapshot>(
                databaseStream,
                cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException("Backupfilen er tom eller ugyldig.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Backupfilformat er ugyldigt for PostgreSQL. Brug en JSON-backup fra denne version.",
                ex);
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            using var tx = connection.BeginTransaction();

            await using (var truncate = connection.CreateCommand())
            {
                truncate.Transaction = tx;
                truncate.CommandText = "TRUNCATE TABLE ScanEntries, AuditEntries, PalletItems, Pallets RESTART IDENTITY;";
                await truncate.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var row in snapshot.Pallets)
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO Pallets(PalletId, GroupKey, ProductNumber, ExpiryDate, TotalQuantity, IsClosed, CreatedAt)
                    VALUES(@id, @key, @product, @expiry, @qty, @closed, @created);
                    """;
                cmd.Parameters.AddWithValue("@id", row.PalletId);
                cmd.Parameters.AddWithValue("@key", row.GroupKey);
                cmd.Parameters.AddWithValue("@product", row.ProductNumber);
                cmd.Parameters.AddWithValue("@expiry", row.ExpiryDate);
                cmd.Parameters.AddWithValue("@qty", row.TotalQuantity);
                cmd.Parameters.AddWithValue("@closed", row.IsClosed);
                cmd.Parameters.AddWithValue("@created", row.CreatedAt);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var row in snapshot.PalletItems)
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO PalletItems(Id, PalletId, ProductNumber, ExpiryDate, Quantity)
                    VALUES(@id, @palletId, @product, @expiry, @qty);
                    """;
                cmd.Parameters.AddWithValue("@id", row.Id);
                cmd.Parameters.AddWithValue("@palletId", row.PalletId);
                cmd.Parameters.AddWithValue("@product", row.ProductNumber);
                cmd.Parameters.AddWithValue("@expiry", row.ExpiryDate);
                cmd.Parameters.AddWithValue("@qty", row.Quantity);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var row in snapshot.ScanEntries)
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO ScanEntries(Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt)
                    VALUES(@id, @ts, @product, @expiry, @qty, @palletId, @groupKey, @createdNew, @confirmedQty, @confirmedMoved, @confirmedAt);
                    """;
                cmd.Parameters.AddWithValue("@id", row.Id);
                cmd.Parameters.AddWithValue("@ts", row.Timestamp);
                cmd.Parameters.AddWithValue("@product", row.ProductNumber);
                cmd.Parameters.AddWithValue("@expiry", row.ExpiryDate);
                cmd.Parameters.AddWithValue("@qty", row.Quantity);
                cmd.Parameters.AddWithValue("@palletId", row.PalletId);
                cmd.Parameters.AddWithValue("@groupKey", row.GroupKey);
                cmd.Parameters.AddWithValue("@createdNew", row.CreatedNewPallet);
                cmd.Parameters.AddWithValue("@confirmedQty", row.ConfirmedQuantity);
                cmd.Parameters.AddWithValue("@confirmedMoved", row.ConfirmedMoved);
                cmd.Parameters.AddWithValue("@confirmedAt", row.ConfirmedAt ?? (object)DBNull.Value);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var row in snapshot.AuditEntries)
            {
                await using var cmd = connection.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = """
                    INSERT INTO AuditEntries(Id, Timestamp, Action, Details, MachineName)
                    VALUES(@id, @ts, @action, @details, @machine);
                    """;
                cmd.Parameters.AddWithValue("@id", row.Id);
                cmd.Parameters.AddWithValue("@ts", row.Timestamp);
                cmd.Parameters.AddWithValue("@action", row.Action);
                cmd.Parameters.AddWithValue("@details", row.Details);
                cmd.Parameters.AddWithValue("@machine", row.MachineName);
                await cmd.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var syncSequence = connection.CreateCommand())
            {
                syncSequence.Transaction = tx;
                syncSequence.CommandText = """
                    SELECT setval(pg_get_serial_sequence('PalletItems', 'Id'), COALESCE((SELECT MAX(Id) FROM PalletItems), 1), (SELECT COUNT(1) > 0 FROM PalletItems));
                    SELECT setval(pg_get_serial_sequence('ScanEntries', 'Id'), COALESCE((SELECT MAX(Id) FROM ScanEntries), 1), (SELECT COUNT(1) > 0 FROM ScanEntries));
                    SELECT setval(pg_get_serial_sequence('AuditEntries', 'Id'), COALESCE((SELECT MAX(Id) FROM AuditEntries), 1), (SELECT COUNT(1) > 0 FROM AuditEntries));
                    """;
                await syncSequence.ExecuteNonQueryAsync(cancellationToken);
            }

            await InsertAuditEntryAsync(connection, tx, "RESTORE_DATABASE", "Database restored from uploaded backup", cancellationToken);
            await tx.CommitAsync(cancellationToken);
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
            WHERE PalletId = @id
            ORDER BY ProductNumber, ExpiryDate;
            """;
        cmd.Parameters.AddWithValue("@id", palletId);
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

    private static async Task<List<PalletBackupRow>> ReadPalletRowsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var list = new List<PalletBackupRow>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT PalletId, GroupKey, ProductNumber, ExpiryDate, TotalQuantity, IsClosed, CreatedAt
            FROM Pallets
            ORDER BY PalletId;
            """;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PalletBackupRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetBoolean(5),
                reader.GetDateTime(6)));
        }

        return list;
    }

    private static async Task<List<PalletItemBackupRow>> ReadPalletItemRowsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var list = new List<PalletItemBackupRow>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, PalletId, ProductNumber, ExpiryDate, Quantity
            FROM PalletItems
            ORDER BY Id;
            """;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new PalletItemBackupRow(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4)));
        }

        return list;
    }

    private static async Task<List<ScanEntryBackupRow>> ReadScanEntryRowsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var list = new List<ScanEntryBackupRow>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt
            FROM ScanEntries
            ORDER BY Id;
            """;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new ScanEntryBackupRow(
                reader.GetInt64(0),
                reader.GetDateTime(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetBoolean(7),
                reader.GetInt32(8),
                reader.GetBoolean(9),
                reader.IsDBNull(10) ? null : reader.GetDateTime(10)));
        }

        return list;
    }

    private static async Task<List<AuditEntryBackupRow>> ReadAuditRowsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var list = new List<AuditEntryBackupRow>();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Timestamp, Action, Details, MachineName
            FROM AuditEntries
            ORDER BY Id;
            """;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new AuditEntryBackupRow(
                reader.GetInt64(0),
                reader.GetDateTime(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4)));
        }

        return list;
    }

    private sealed record PostgresBackupSnapshot(
        List<PalletBackupRow> Pallets,
        List<PalletItemBackupRow> PalletItems,
        List<ScanEntryBackupRow> ScanEntries,
        List<AuditEntryBackupRow> AuditEntries);

    private sealed record PalletBackupRow(
        string PalletId,
        string GroupKey,
        string ProductNumber,
        string ExpiryDate,
        int TotalQuantity,
        bool IsClosed,
        DateTime CreatedAt);

    private sealed record PalletItemBackupRow(
        long Id,
        string PalletId,
        string ProductNumber,
        string ExpiryDate,
        int Quantity);

    private sealed record ScanEntryBackupRow(
        long Id,
        DateTime Timestamp,
        string ProductNumber,
        string ExpiryDate,
        int Quantity,
        string PalletId,
        string GroupKey,
        bool CreatedNewPallet,
        int ConfirmedQuantity,
        bool ConfirmedMoved,
        DateTime? ConfirmedAt);

    private sealed record AuditEntryBackupRow(
        long Id,
        DateTime Timestamp,
        string Action,
        string Details,
        string MachineName);

    public void Dispose()
    {
        _writeLock.Dispose();
    }
}

