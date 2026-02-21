using LagerPalleSortering.Api;
using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Application.Services;
using LagerPalleSortering.Domain;
using LagerPalleSortering.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
var disableHttpsRedirection = builder.Configuration.GetValue<bool>("DisableHttpsRedirection");

builder.Services.Configure<WarehouseRulesOptions>(builder.Configuration.GetSection(WarehouseRulesOptions.SectionName));
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddWarehouseStorage(builder.Configuration);
builder.Services.AddSingleton<IProductBarcodeNormalizer, DefaultProductBarcodeNormalizer>();
builder.Services.AddSingleton<IPalletBarcodeService, DefaultPalletBarcodeService>();
builder.Services.AddSingleton<IOperationalMetrics, OperationalMetricsService>();
builder.Services.AddSingleton<IWarehouseDataService, WarehouseDataService>();
builder.Services.AddSingleton<IWarehouseExportService, WarehouseExportService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "lagerpalle.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                context.Request.Path.StartsWithSegments("/auth", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect("/login");
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

var authOptions = app.Services.GetRequiredService<IOptions<AuthOptions>>().Value;
if (authOptions.RequireAuthentication && authOptions.Users.Count == 0)
{
    throw new InvalidOperationException("Auth er aktiveret, men ingen brugere er konfigureret under Auth:Users.");
}

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
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
               && !context.Request.Path.StartsWithSegments("/auth", StringComparison.OrdinalIgnoreCase),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
if (!disableHttpsRedirection)
{
    app.UseHttpsRedirection();
}

// Required so Vite-built React assets in wwwroot/app/assets are served in prod/test.
app.UseStaticFiles();
// Add request correlation early so all downstream logs/endpoints share the same ID.
app.UseRequestCorrelation();
app.UseAuthentication();
app.UseAuthorization();
app.MapAuthApiEndpoints();

var protectedEndpoints = authOptions.RequireAuthentication
    ? app.MapGroup(string.Empty).RequireAuthorization()
    : app.MapGroup(string.Empty);
protectedEndpoints.MapWarehouseApiEndpoints();
protectedEndpoints.MapOperationalApiEndpoints();

app.MapGet("/", () => Results.Redirect("/app"));
// Let React Router own non-file routes under /app (e.g. /app/history).
app.MapFallbackToFile("/app/{*path:nonfile}", "app/index.html");
app.MapFallbackToFile("/login/{*path:nonfile}", "app/index.html");

app.Run();

public partial class Program
{
}
