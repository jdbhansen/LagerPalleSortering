using LagerPalleSortering.Components;
using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Application.Services;
using LagerPalleSortering.Domain;
using LagerPalleSortering.Infrastructure.Repositories;
using LagerPalleSortering.Services;

var builder = WebApplication.CreateBuilder(args);
var disableHttpsRedirection = builder.Configuration.GetValue<bool>("DisableHttpsRedirection");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.Configure<WarehouseRulesOptions>(builder.Configuration.GetSection(WarehouseRulesOptions.SectionName));
builder.Services.AddSingleton<IWarehouseRepository, SqliteWarehouseRepository>();
builder.Services.AddSingleton<IProductBarcodeNormalizer, DefaultProductBarcodeNormalizer>();
builder.Services.AddSingleton<IPalletBarcodeService, DefaultPalletBarcodeService>();
builder.Services.AddSingleton<IOperationalMetrics, OperationalMetricsService>();
builder.Services.AddSingleton<IWarehouseDataService, WarehouseDataService>();
builder.Services.AddSingleton<IWarehouseExportService, WarehouseExportService>();
builder.Services.AddSingleton<BarcodeService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // Initialize DB schema/migrations before accepting requests.
    var dataService = scope.ServiceProvider.GetRequiredService<IWarehouseDataService>();
    await dataService.InitializeAsync(app.Lifetime.ApplicationStopping);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
if (!disableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/export/csv", async (IWarehouseExportService exportService, CancellationToken cancellationToken) =>
{
    var file = await exportService.ExportCsvAsync(cancellationToken);
    // Local timestamp in filename makes operator downloads easier to identify.
    var fileName = $"lager-export-{DateTime.Now:yyyyMMdd-HHmm}.csv";
    return Results.File(file, "text/csv; charset=utf-8", fileName);
});

app.MapGet("/export/excel", async (IWarehouseExportService exportService, CancellationToken cancellationToken) =>
{
    var file = await exportService.ExportExcelAsync(cancellationToken);
    var fileName = $"lager-export-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";
    return Results.File(
        file,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
});

app.MapGet("/backup/db", async (IWarehouseDataService dataService, CancellationToken cancellationToken) =>
{
    var file = await dataService.BackupDatabaseAsync(cancellationToken);
    var fileName = $"lager-backup-{DateTime.Now:yyyyMMdd-HHmm}.db";
    return Results.File(file, "application/octet-stream", fileName);
});

app.MapGet("/health", async (IWarehouseDataService dataService, IOperationalMetrics metrics, CancellationToken cancellationToken) =>
{
    var snapshot = await dataService.GetHealthSnapshotAsync(cancellationToken);
    var metricSnapshot = metrics.GetSnapshot();
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
});

app.MapGet("/metrics", (IOperationalMetrics metrics) =>
{
    var snapshot = metrics.GetSnapshot();
    return Results.Ok(snapshot);
});

app.Run();
