using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Api;

public static class WarehouseApiEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/warehouse/dashboard", GetDashboardAsync);
        endpoints.MapGet("/api/warehouse/pallets/{palletId}", GetPalletAsync);
        endpoints.MapGet("/api/warehouse/pallets/{palletId}/contents", GetPalletContentsAsync);
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

        var type = result.Success ? WarehouseOperationTypes.Success : WarehouseOperationTypes.Error;
        return Results.Ok(new WarehouseOperationApiResponse(type, result.Message, result.PalletId, CreatedNewPallet: result.CreatedNewPallet));
    }

    private static async Task<IResult> GetPalletAsync(
        IWarehouseDataService dataService,
        string palletId,
        CancellationToken cancellationToken)
    {
        var pallet = await dataService.GetPalletForPrintAsync(palletId, cancellationToken);
        if (pallet is null)
        {
            return Results.NotFound(new WarehouseOperationApiResponse(WarehouseOperationTypes.Error, $"Palle {palletId} findes ikke."));
        }

        return Results.Ok(pallet);
    }

    private static async Task<IResult> GetPalletContentsAsync(
        IWarehouseDataService dataService,
        string palletId,
        CancellationToken cancellationToken)
    {
        var items = await dataService.GetPalletContentsAsync(palletId, cancellationToken);
        return Results.Ok(new WarehousePalletContentsApiResponse(items));
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
            return Results.BadRequest(CreateBatchOperationResponse(batchResult));
        }

        return Results.Ok(CreateBatchOperationResponse(batchResult));
    }

    private static async Task<IResult> ClosePalletAsync(
        IWarehouseDataService dataService,
        string palletId,
        CancellationToken cancellationToken)
    {
        await dataService.ClosePalletAsync(palletId, cancellationToken);
        return Results.Ok(new WarehouseOperationApiResponse(WarehouseOperationTypes.Success, $"Palle {palletId} er lukket.", palletId));
    }

    private static async Task<IResult> UndoLastAsync(
        IWarehouseDataService dataService,
        CancellationToken cancellationToken)
    {
        var undo = await dataService.UndoLastAsync(cancellationToken);
        if (undo is null)
        {
            return Results.Ok(new WarehouseOperationApiResponse(WarehouseOperationTypes.Error, "Der er intet at fortryde."));
        }

        return Results.Ok(new WarehouseOperationApiResponse(
            WarehouseOperationTypes.Success,
            $"Fortrudt: {undo.Quantity} kolli fjernet fra {undo.PalletId}.",
            undo.PalletId));
    }

    private static async Task<IResult> ClearDatabaseAsync(
        IWarehouseDataService dataService,
        CancellationToken cancellationToken)
    {
        await dataService.ClearAllDataAsync(cancellationToken);
        return Results.Ok(new WarehouseOperationApiResponse(WarehouseOperationTypes.Success, "Databasen er ryddet."));
    }

    private static async Task<IResult> RestoreDatabaseAsync(
        IWarehouseDataService dataService,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new WarehouseOperationApiResponse(WarehouseOperationTypes.Error, "Upload backup som multipart/form-data."));
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new WarehouseOperationApiResponse(WarehouseOperationTypes.Error, "Vælg en backupfil først."));
        }

        await using var stream = file.OpenReadStream();
        await dataService.RestoreDatabaseAsync(stream, cancellationToken);
        return Results.Ok(new WarehouseOperationApiResponse(WarehouseOperationTypes.Success, "Database gendannet fra backup."));
    }

    private static WarehouseOperationApiResponse CreateBatchOperationResponse(MoveBatchConfirmationResult result) =>
        new(
            result.Status,
            result.Message,
            result.PalletId,
            result.Confirmed,
            result.Requested);
}
