using System.Net;
using System.Net.Http.Json;
using System.Text;
using LagerPalleSortering.Api;
using LagerPalleSortering.Tests.TestInfrastructure;

namespace LagerPalleSortering.Tests;

public sealed class WarehouseApiEndpointsTests
{
    [Fact]
    public async Task RootEndpoint_RedirectsToApp()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/app", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task AppFallback_ForClientRoute_ReturnsIndexHtml()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/app/warehouse/history");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<!doctype html>", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DashboardEndpoint_WhenFreshDatabase_ReturnsEmptyCollections()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/warehouse/dashboard");
        var payload = await response.Content.ReadFromJsonAsync<WarehouseDashboardApiResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Empty(payload.OpenPallets);
        Assert.Empty(payload.Entries);
    }

    [Fact]
    public async Task DashboardEndpoint_V1Route_WhenFreshDatabase_ReturnsEmptyCollections()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/warehouse/dashboard");
        var payload = await response.Content.ReadFromJsonAsync<WarehouseDashboardApiResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Empty(payload.OpenPallets);
        Assert.Empty(payload.Entries);
    }

    [Fact]
    public async Task RegisterAndConfirmFlow_UpdatesDashboardAndReturnsExpectedStatus()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync(
            "/api/warehouse/register",
            new RegisterColliApiRequest("api-100", "20270101", 2));
        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<WarehouseOperationApiResponse>();

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);
        Assert.NotNull(registerPayload);
        Assert.Equal("success", registerPayload.Type);
        Assert.False(string.IsNullOrWhiteSpace(registerPayload.PalletId));

        var confirmResponse = await client.PostAsJsonAsync(
            "/api/warehouse/confirm",
            new ConfirmMoveApiRequest($"PALLET:{registerPayload.PalletId}", 2));
        var confirmPayload = await confirmResponse.Content.ReadFromJsonAsync<WarehouseOperationApiResponse>();

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);
        Assert.NotNull(confirmPayload);
        Assert.Equal("success", confirmPayload.Type);
        Assert.Equal(2, confirmPayload.Confirmed);
        Assert.Equal(2, confirmPayload.Requested);

        var dashboardResponse = await client.GetAsync("/api/warehouse/dashboard");
        var dashboardPayload = await dashboardResponse.Content.ReadFromJsonAsync<WarehouseDashboardApiResponse>();

        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.NotNull(dashboardPayload);
        Assert.Single(dashboardPayload.OpenPallets);
        Assert.NotEmpty(dashboardPayload.Entries);
        Assert.Equal(2, dashboardPayload.Entries[0].ConfirmedQuantity);
        Assert.True(dashboardPayload.Entries[0].ConfirmedMoved);
    }

    [Fact]
    public async Task UndoAndClearEndpoints_PreserveConsistentState()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/warehouse/register", new RegisterColliApiRequest("api-undo", "20270101", 1));

        var undoResponse = await client.PostAsync("/api/warehouse/undo", null);
        var undoPayload = await undoResponse.Content.ReadFromJsonAsync<WarehouseOperationApiResponse>();
        Assert.Equal(HttpStatusCode.OK, undoResponse.StatusCode);
        Assert.NotNull(undoPayload);
        Assert.Equal("success", undoPayload.Type);

        var clearResponse = await client.PostAsync("/api/warehouse/clear", null);
        var clearPayload = await clearResponse.Content.ReadFromJsonAsync<WarehouseOperationApiResponse>();
        Assert.Equal(HttpStatusCode.OK, clearResponse.StatusCode);
        Assert.NotNull(clearPayload);
        Assert.Equal("success", clearPayload.Type);

        var dashboardResponse = await client.GetAsync("/api/warehouse/dashboard");
        var dashboardPayload = await dashboardResponse.Content.ReadFromJsonAsync<WarehouseDashboardApiResponse>();

        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.NotNull(dashboardPayload);
        Assert.Empty(dashboardPayload.OpenPallets);
        Assert.Empty(dashboardPayload.Entries);
    }

    [Fact]
    public async Task BackupAndRestoreEndpoints_RestoreDatabaseState()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/warehouse/register", new RegisterColliApiRequest("api-restore", "20271224", 1));

        var backupResponse = await client.GetAsync("/backup/db");
        var backupBytes = await backupResponse.Content.ReadAsByteArrayAsync();
        Assert.Equal(HttpStatusCode.OK, backupResponse.StatusCode);
        Assert.NotEmpty(backupBytes);

        await client.PostAsync("/api/warehouse/clear", null);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(backupBytes), "file", "lager-backup.db");
        var restoreResponse = await client.PostAsync("/api/warehouse/restore", content);
        var restorePayload = await restoreResponse.Content.ReadFromJsonAsync<WarehouseOperationApiResponse>();

        Assert.Equal(HttpStatusCode.OK, restoreResponse.StatusCode);
        Assert.NotNull(restorePayload);
        Assert.Equal("success", restorePayload.Type);

        var dashboardResponse = await client.GetAsync("/api/warehouse/dashboard");
        var dashboardPayload = await dashboardResponse.Content.ReadFromJsonAsync<WarehouseDashboardApiResponse>();
        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.NotNull(dashboardPayload);
        Assert.Single(dashboardPayload.OpenPallets);
    }

    [Fact]
    public async Task ConfirmEndpoint_WithInvalidCount_ReturnsBadRequest()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/warehouse/confirm",
            new ConfirmMoveApiRequest("PALLET:P-001", 0));
        var payload = await response.Content.ReadFromJsonAsync<WarehouseOperationApiResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("error", payload.Type);
    }

    [Fact]
    public async Task RegisterEndpoint_WithWhitespaceProduct_ReturnsErrorResponse()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/warehouse/register",
            new RegisterColliApiRequest("   ", "20270101", 1));
        var payload = await response.Content.ReadFromJsonAsync<WarehouseOperationApiResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("error", payload.Type);
        Assert.Contains("Varenummer mangler", payload.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterEndpoint_WithWhitespaceExpiry_AllowsNoExpiryRegistration()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/warehouse/register",
            new RegisterColliApiRequest("api-noexp", "   ", 1));
        var payload = await response.Content.ReadFromJsonAsync<WarehouseOperationApiResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("success", payload.Type);
        Assert.False(string.IsNullOrWhiteSpace(payload.PalletId));
    }

    [Fact]
    public async Task RestoreEndpoint_WithoutFile_ReturnsBadRequest()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Array.Empty<byte>()), "file", "empty.db");
        var response = await client.PostAsync("/api/warehouse/restore", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Vælg en backupfil først", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RestoreEndpoint_WithNonFormPayload_ReturnsBadRequest()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var content = new StringContent("""{"file":"not-a-file"}""", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/warehouse/restore", content);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("multipart/form-data", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HealthAndMetricsEndpoints_ReturnSnapshots()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        var healthResponse = await client.GetAsync("/health");
        var healthBody = await healthResponse.Content.ReadAsStringAsync();

        var metricsResponse = await client.GetAsync("/metrics");
        var metricsBody = await metricsResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Contains("\"status\"", healthBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"warehouse\"", healthBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"metrics\"", healthBody, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
        Assert.Contains("registerAttempts", metricsBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestCorrelationHeader_IsReturnedFromRequests()
    {
        using var factory = new WarehouseApiWebApplicationFactory();
        using var client = factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(RequestCorrelationMiddlewareExtensions.CorrelationHeaderName, "test-correlation-id");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues(RequestCorrelationMiddlewareExtensions.CorrelationHeaderName, out var values));
        Assert.Contains("test-correlation-id", values);
    }

    [Fact]
    public async Task AuthEndpoints_WithValidCredentials_LoginAndMeReturnAuthenticated()
    {
        using var factory = new WarehouseApiWebApplicationFactory(disableAuth: false, testUsername: "tester", testPassword: "secret-123");
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { username = "tester", password = "secret-123" });
        var meResponse = await client.GetAsync("/auth/me");
        var meBody = await meResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.Contains("\"authenticated\":true", meBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"username\":\"tester\"", meBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AuthEndpoints_WithInvalidCredentials_ReturnUnauthorized()
    {
        using var factory = new WarehouseApiWebApplicationFactory(disableAuth: false, testUsername: "tester", testPassword: "secret-123");
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { username = "tester", password = "wrong" });
        var meResponse = await client.GetAsync("/auth/me");
        var meBody = await meResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.Contains("\"authenticated\":false", meBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProtectedApi_WhenAuthEnabledAndNotLoggedIn_ReturnsUnauthorized()
    {
        using var factory = new WarehouseApiWebApplicationFactory(disableAuth: false, testUsername: "tester", testPassword: "secret-123");
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/warehouse/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthEndpoints_Logout_ClearsAuthentication()
    {
        using var factory = new WarehouseApiWebApplicationFactory(disableAuth: false, testUsername: "tester", testPassword: "secret-123");
        using var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new { username = "tester", password = "secret-123" });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var logoutResponse = await client.PostAsync("/auth/logout", null);
        var meAfterLogout = await client.GetAsync("/auth/me");
        var meBody = await meAfterLogout.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, meAfterLogout.StatusCode);
        Assert.Contains("\"authenticated\":false", meBody, StringComparison.OrdinalIgnoreCase);
    }
}
