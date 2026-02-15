using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;
using Microsoft.Data.Sqlite;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class SqliteWarehouseRepository : IWarehouseRepository, IDisposable
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

    public async Task<(string PalletId, bool CreatedNewPallet)> RegisterAsync(string productNumber, string expiryDate, int quantity, CancellationToken cancellationToken = default)
    {
        await writeLock.WaitAsync(cancellationToken);
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

            await tx.CommitAsync(cancellationToken);
            return (pallet.PalletId, createdNew);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task ClosePalletAsync(string palletId, CancellationToken cancellationToken = default)
    {
        await writeLock.WaitAsync(cancellationToken);
        try
        {
            await using var connection = OpenConnection();
            await connection.OpenAsync(cancellationToken);
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Pallets SET IsClosed = 1 WHERE PalletId = $id;";
            cmd.Parameters.AddWithValue("$id", palletId);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<long?> ConfirmLatestUnconfirmedByPalletIdAsync(string palletId, DateTime confirmedAtUtc, CancellationToken cancellationToken = default)
    {
        await writeLock.WaitAsync(cancellationToken);
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
            await tx.CommitAsync(cancellationToken);
            return entry.Id;
        }
        finally
        {
            writeLock.Release();
        }
    }

    public async Task<UndoResult?> UndoLastAsync(CancellationToken cancellationToken = default)
    {
        await writeLock.WaitAsync(cancellationToken);
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
                    await DeletePalletItemsAsync(connection, tx, pallet.PalletId, cancellationToken);
                    await DeletePalletAsync(connection, tx, pallet.PalletId, cancellationToken);
                }
                else
                {
                    await UpdatePalletQuantityAsync(connection, tx, pallet.PalletId, Math.Max(0, updated), cancellationToken);
                }
            }

            await tx.CommitAsync(cancellationToken);
            return new UndoResult(entry.PalletId, entry.Quantity);
        }
        finally
        {
            writeLock.Release();
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
            ORDER BY p.PalletId;
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

    public async Task<PalletRecord?> GetPalletByIdAsync(string palletId, CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        return await GetPalletByIdAsync(connection, null, palletId, cancellationToken);
    }

    public void Dispose()
    {
        writeLock.Dispose();
    }
}
