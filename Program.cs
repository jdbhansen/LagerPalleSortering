using LagerPalleSortering.Api;
using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Application.Services;
using LagerPalleSortering.Domain;
using LagerPalleSortering.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);
var disableHttpsRedirection = builder.Configuration.GetValue<bool>("DisableHttpsRedirection");

builder.Services.Configure<WarehouseRulesOptions>(builder.Configuration.GetSection(WarehouseRulesOptions.SectionName));
builder.Services.AddSingleton<IWarehouseRepository, SqliteWarehouseRepository>();
builder.Services.AddSingleton<IProductBarcodeNormalizer, DefaultProductBarcodeNormalizer>();
builder.Services.AddSingleton<IPalletBarcodeService, DefaultPalletBarcodeService>();
builder.Services.AddSingleton<IOperationalMetrics, OperationalMetricsService>();
builder.Services.AddSingleton<IWarehouseDataService, WarehouseDataService>();
builder.Services.AddSingleton<IWarehouseExportService, WarehouseExportService>();

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

// Required so Vite-built React assets in wwwroot/app/assets are served in prod/test.
app.UseStaticFiles();
app.MapWarehouseApiEndpoints();
app.MapOperationalApiEndpoints();

app.MapGet("/", () => Results.Redirect("/app"));
// Let React Router own non-file routes under /app (e.g. /app/history).
app.MapFallbackToFile("/app/{*path:nonfile}", "app/index.html");

app.Run();

public partial class Program
{
}
