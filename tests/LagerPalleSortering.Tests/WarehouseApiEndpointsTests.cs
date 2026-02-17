using System.Net;
using System.Net.Http.Json;
using LagerPalleSortering.Api;
using LagerPalleSortering.Tests.TestInfrastructure;

namespace LagerPalleSortering.Tests;

public sealed class WarehouseApiEndpointsTests
{
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
}
