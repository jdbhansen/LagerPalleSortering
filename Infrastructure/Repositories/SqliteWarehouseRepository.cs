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
                0,
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
            SELECT Id, Timestamp, ProductNumber, ExpiryDate, Quantity, PalletId, GroupKey, CreatedNewPallet, ConfirmedQuantity, ConfirmedMoved, ConfirmedAt
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

    public void Dispose()
    {
        writeLock.Dispose();
    }
}
