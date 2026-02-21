using LagerPalleSortering.Domain;
using Npgsql;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class PostgresWarehouseRepository
{
    private NpgsqlConnection OpenConnection() => new(_connectionString);

    private static string BuildKey(string product, string expiry) => $"{product}|{expiry}";

    private static PalletRecord ReadPalletSummary(NpgsqlDataReader reader)
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
            reader.GetBoolean(2),
            reader.GetDateTime(3));
    }

    private static ScanEntryRecord ReadEntry(NpgsqlDataReader reader) =>
        new(
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
            reader.IsDBNull(10) ? null : reader.GetDateTime(10));
}

