namespace LagerPalleSortering.Domain;

public static class WarehouseConstants
{
    // Placeholder for products that have no stated expiration date.
    public const string NoExpiry = "NOEXP";
    // Number of latest entries shown in the home dashboard.
    public const int DefaultRecentEntries = 12;
    // Safety cap for export size to avoid excessive memory pressure.
    public const int MaxExportRows = 200000;
}
