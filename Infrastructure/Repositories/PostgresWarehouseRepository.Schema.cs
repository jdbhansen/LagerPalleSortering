using System.Globalization;
using Npgsql;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class PostgresWarehouseRepository
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = """
            CREATE TABLE IF NOT EXISTS Pallets (
                PalletId TEXT PRIMARY KEY,
                GroupKey TEXT NOT NULL,
                ProductNumber TEXT NOT NULL,
                ExpiryDate TEXT NOT NULL,
                TotalQuantity INTEGER NOT NULL,
                IsClosed BOOLEAN NOT NULL,
                CreatedAt TIMESTAMPTZ NOT NULL
            );

            CREATE TABLE IF NOT EXISTS PalletItems (
                Id BIGSERIAL PRIMARY KEY,
                PalletId TEXT NOT NULL,
                ProductNumber TEXT NOT NULL,
                ExpiryDate TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                UNIQUE(PalletId, ProductNumber, ExpiryDate)
            );

            CREATE TABLE IF NOT EXISTS ScanEntries (
                Id BIGSERIAL PRIMARY KEY,
                Timestamp TIMESTAMPTZ NOT NULL,
                ProductNumber TEXT NOT NULL,
                ExpiryDate TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                PalletId TEXT NOT NULL,
                GroupKey TEXT NOT NULL,
                CreatedNewPallet BOOLEAN NOT NULL,
                ConfirmedQuantity INTEGER NOT NULL DEFAULT 0,
                ConfirmedMoved BOOLEAN NOT NULL DEFAULT FALSE,
                ConfirmedAt TIMESTAMPTZ NULL
            );

            CREATE TABLE IF NOT EXISTS AuditEntries (
                Id BIGSERIAL PRIMARY KEY,
                Timestamp TIMESTAMPTZ NOT NULL,
                Action TEXT NOT NULL,
                Details TEXT NOT NULL,
                MachineName TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Pallets_GroupKey_Open ON Pallets(GroupKey, IsClosed);
            CREATE INDEX IF NOT EXISTS IX_PalletItems_PalletId ON PalletItems(PalletId);
            CREATE INDEX IF NOT EXISTS IX_ScanEntries_Id ON ScanEntries(Id DESC);
            CREATE INDEX IF NOT EXISTS IX_AuditEntries_Id ON AuditEntries(Id DESC);
            """;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        await EnsureColumnExistsAsync(connection, "ScanEntries", "ConfirmedMoved", "BOOLEAN NOT NULL DEFAULT FALSE", cancellationToken);
        await EnsureColumnExistsAsync(connection, "ScanEntries", "ConfirmedAt", "TIMESTAMPTZ NULL", cancellationToken);
        await EnsureColumnExistsAsync(connection, "ScanEntries", "ConfirmedQuantity", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await MigrateLegacyConfirmedQuantityAsync(connection, cancellationToken);
        await MigrateLegacyPalletRowsToPalletItemsAsync(connection, cancellationToken);
    }

    private static async Task EnsureColumnExistsAsync(NpgsqlConnection connection, string table, string column, string definitionSql, CancellationToken cancellationToken)
    {
        await using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN IF NOT EXISTS {column} {definitionSql};";
        await alter.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MigrateLegacyConfirmedQuantityAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        // Backfill old boolean confirmation model into quantity-based confirmation model.
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            UPDATE ScanEntries
            SET ConfirmedQuantity = CASE
                WHEN ConfirmedMoved = TRUE THEN Quantity
                ELSE 0
            END
            WHERE ConfirmedQuantity = 0;
            """;
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task MigrateLegacyPalletRowsToPalletItemsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var countCmd = connection.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(1) FROM PalletItems;";
        var itemCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (itemCount > 0)
        {
            return;
        }

        // One-time backfill from pre-variant schema.
        await using var migrateCmd = connection.CreateCommand();
        migrateCmd.CommandText = """
            INSERT INTO PalletItems(PalletId, ProductNumber, ExpiryDate, Quantity)
            SELECT PalletId, ProductNumber, ExpiryDate, TotalQuantity
            FROM Pallets
            WHERE ProductNumber IS NOT NULL AND ProductNumber <> '';
            """;
        await migrateCmd.ExecuteNonQueryAsync(cancellationToken);
    }
}

