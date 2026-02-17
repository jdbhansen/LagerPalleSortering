using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Api;

public static class WarehouseApiEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/warehouse/dashboard", GetDashboardAsync);
        // Scanner/SPA clients do not post antiforgery tokens; endpoints are same-origin and auth-less by design.
        endpoints.MapPost("/api/warehouse/register", RegisterColliAsync).DisableAntiforgery();
        endpoints.MapPost("/api/warehouse/confirm", ConfirmMoveAsync).DisableAntiforgery();
        endpoints.MapPost("/api/warehouse/pallets/{palletId}/close", ClosePalletAsync).DisableAntiforgery();
        endpoints.MapPost("/api/warehouse/undo", UndoLastAsync).DisableAntiforgery();
        endpoints.MapPost("/api/warehouse/clear", ClearDatabaseAsync).DisableAntiforgery();
        endpoints.MapPost("/api/warehouse/restore", RestoreDatabaseAsync).DisableAntiforgery();

        return endpoints;
    }

    private static async Task<IResult> GetDashboardAsync(
        IWarehouseDataService dataService,
        CancellationToken cancellationToken)
    {
        var openPallets = await dataService.GetOpenPalletsAsync(cancellationToken);
        var entries = await dataService.GetRecentEntriesAsync(WarehouseConstants.DefaultRecentEntries, cancellationToken);
        return Results.Ok(new WarehouseDashboardApiResponse(openPallets, entries));
    }

    private static async Task<IResult> RegisterColliAsync(
        IWarehouseDataService dataService,
        RegisterColliApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await dataService.RegisterColliAsync(
            request.ProductNumber ?? string.Empty,
            request.ExpiryDateRaw,
            request.Quantity,
            cancellationToken);

        var type = result.Success ? "success" : "error";
        return Results.Ok(new WarehouseOperationApiResponse(type, result.Message, result.PalletId));
    }

    private static async Task<IResult> ConfirmMoveAsync(
        IWarehouseDataService dataService,
        ConfirmMoveApiRequest request,
        CancellationToken cancellationToken)
    {
        // ConfirmScanCount allows one scan to confirm multiple physical colli in sequence.
        var batchResult = await dataService.ConfirmMoveBatchByPalletScanAsync(
            request.ScannedPalletCode ?? string.Empty,
            request.ConfirmScanCount,
            cancellationToken);

        if (request.ConfirmScanCount <= 0)
        {
            return Results.BadRequest(new WarehouseOperationApiResponse(
                batchResult.Status,
                batchResult.Message,
                batchResult.PalletId,
                batchResult.Confirmed,
                batchResult.Requested));
        }

        return Results.Ok(new WarehouseOperationApiResponse(
            batchResult.Status,
            batchResult.Message,
            batchResult.PalletId,
            batchResult.Confirmed,
            batchResult.Requested));
    }

    private static async Task<IResult> ClosePalletAsync(
        IWarehouseDataService dataService,
        string palletId,
        CancellationToken cancellationToken)
    {
        await dataService.ClosePalletAsync(palletId, cancellationToken);
        return Results.Ok(new WarehouseOperationApiResponse("success", $"Palle {palletId} er lukket.", palletId));
    }

    private static async Task<IResult> UndoLastAsync(
        IWarehouseDataService dataService,
        CancellationToken cancellationToken)
    {
        var undo = await dataService.UndoLastAsync(cancellationToken);
        if (undo is null)
        {
            return Results.Ok(new WarehouseOperationApiResponse("error", "Der er intet at fortryde."));
        }

        return Results.Ok(new WarehouseOperationApiResponse(
            "success",
            $"Fortrudt: {undo.Quantity} kolli fjernet fra {undo.PalletId}.",
            undo.PalletId));
    }

    private static async Task<IResult> ClearDatabaseAsync(
        IWarehouseDataService dataService,
        CancellationToken cancellationToken)
    {
        await dataService.ClearAllDataAsync(cancellationToken);
        return Results.Ok(new WarehouseOperationApiResponse("success", "Databasen er ryddet."));
    }

    private static async Task<IResult> RestoreDatabaseAsync(
        IWarehouseDataService dataService,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new WarehouseOperationApiResponse("error", "Vælg en backupfil først."));
        }

        await using var stream = file.OpenReadStream();
        await dataService.RestoreDatabaseAsync(stream, cancellationToken);
        return Results.Ok(new WarehouseOperationApiResponse("success", "Database gendannet fra backup."));
    }
}
