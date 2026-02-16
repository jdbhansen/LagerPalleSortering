using System.Globalization;
using LagerPalleSortering.Domain;
using Microsoft.Data.Sqlite;

namespace LagerPalleSortering.Infrastructure.Repositories;

public sealed partial class SqliteWarehouseRepository
{
    private SqliteConnection OpenConnection() => new(_connectionString);

    private static string BuildKey(string product, string expiry) => $"{product}|{expiry}";

    private static PalletRecord ReadPalletSummary(SqliteDataReader reader)
    {
        var variantCount = reader.GetInt32(4);
        // For mixed pallets, expose a clear synthetic display value in the dashboard.
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
            reader.GetInt32(8),
            reader.GetInt32(9) == 1,
            reader.IsDBNull(10)
                ? null
                : DateTime.Parse(reader.GetString(10), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
}
