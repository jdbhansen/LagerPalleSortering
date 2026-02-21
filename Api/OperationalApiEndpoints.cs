using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Infrastructure.Repositories;
using Microsoft.Extensions.Options;

namespace LagerPalleSortering.Api;

public static class OperationalApiEndpoints
{
    public static IEndpointRouteBuilder MapOperationalApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/export/csv", ExportCsvAsync);
        endpoints.MapGet("/export/excel", ExportExcelAsync);
        endpoints.MapGet("/backup/db", BackupDatabaseAsync);
        endpoints.MapGet("/health", GetHealthAsync);
        endpoints.MapGet("/metrics", GetMetrics);
        return endpoints;
    }

    private static async Task<IResult> ExportCsvAsync(IWarehouseExportService exportService, CancellationToken cancellationToken)
    {
        var file = await exportService.ExportCsvAsync(cancellationToken);
        var fileName = $"lager-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.csv";
        return Results.File(file, "text/csv; charset=utf-8", fileName);
    }

    private static async Task<IResult> ExportExcelAsync(IWarehouseExportService exportService, CancellationToken cancellationToken)
    {
        var file = await exportService.ExportExcelAsync(cancellationToken);
        var fileName = $"lager-export-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return Results.File(
            file,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private static async Task<IResult> BackupDatabaseAsync(
        IWarehouseDataService dataService,
        IOptions<DatabaseOptions> databaseOptions,
        CancellationToken cancellationToken)
    {
        var file = await dataService.BackupDatabaseAsync(cancellationToken);
        var isPostgresBackup = string.Equals(
            databaseOptions.Value.Provider,
            "Postgres",
            StringComparison.OrdinalIgnoreCase);
        var fileName = isPostgresBackup
            ? $"lager-backup-{DateTime.UtcNow:yyyyMMdd-HHmm}.json"
            : $"lager-backup-{DateTime.UtcNow:yyyyMMdd-HHmm}.db";
        var contentType = isPostgresBackup ? "application/json" : "application/octet-stream";
        return Results.File(file, contentType, fileName);
    }

    private static async Task<IResult> GetHealthAsync(
        IWarehouseDataService dataService,
        IOperationalMetrics metrics,
        CancellationToken cancellationToken)
    {
        var snapshot = await dataService.GetHealthSnapshotAsync(cancellationToken);
        var metricSnapshot = metrics.GetSnapshot();
        // "degraded" is intentionally sticky for a short window to surface intermittent failures in monitoring.
        var status = metricSnapshot.LastErrorAtUtc.HasValue &&
                     metricSnapshot.LastErrorAtUtc.Value >= DateTime.UtcNow.AddMinutes(-5)
            ? "degraded"
            : "ok";

        return Results.Ok(new
        {
            status,
            timestampUtc = DateTime.UtcNow,
            warehouse = snapshot,
            metrics = metricSnapshot
        });
    }

    private static IResult GetMetrics(IOperationalMetrics metrics)
    {
        var snapshot = metrics.GetSnapshot();
        return Results.Ok(snapshot);
    }
}
