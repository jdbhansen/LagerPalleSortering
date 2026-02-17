using LagerPalleSortering.Services;
using Microsoft.JSInterop;

namespace LagerPalleSortering.Tests;

public sealed class LagerScannerClientTests
{
    [Fact]
    public async Task InitializeRegistrationFlowAsync_InvokesInitWithExpectedIds()
    {
        var js = new CapturingJsRuntime();
        var client = new LagerScannerClient(js);

        await client.InitializeRegistrationFlowAsync("productInput", "expiryInput", "quantityInput", "registerButton");

        var call = Assert.Single(js.Invocations);
        Assert.Equal("lagerScanner.init", call.Identifier);
        Assert.Equal(new object?[] { "productInput", "expiryInput", "quantityInput", "registerButton" }, call.Arguments);
    }

    [Fact]
    public async Task InitializePalletConfirmAsync_InvokesInitPalletConfirmWithExpectedIds()
    {
        var js = new CapturingJsRuntime();
        var client = new LagerScannerClient(js);

        await client.InitializePalletConfirmAsync("palletScanInput", "confirmMoveButton", "confirmCountInput");

        var call = Assert.Single(js.Invocations);
        Assert.Equal("lagerScanner.initPalletConfirm", call.Identifier);
        Assert.Equal(new object?[] { "palletScanInput", "confirmMoveButton", "confirmCountInput" }, call.Arguments);
    }

    [Fact]
    public async Task InitializeHotkeysAsync_InvokesInitHotkeysWithMappedOptions()
    {
        var js = new CapturingJsRuntime();
        var client = new LagerScannerClient(js);

        await client.InitializeHotkeysAsync(new LagerHotkeysOptions(
            "productInput",
            "palletScanInput",
            "registerButton",
            "confirmMoveButton",
            "undoLastButton",
            "clearCancelButton"));

        var call = Assert.Single(js.Invocations);
        Assert.Equal("lagerScanner.initHotkeys", call.Identifier);
        var optionsPayload = Assert.Single(call.Arguments);
        Assert.Equal("productInput", ReadStringProperty(optionsPayload, "productInputId"));
        Assert.Equal("palletScanInput", ReadStringProperty(optionsPayload, "palletInputId"));
        Assert.Equal("registerButton", ReadStringProperty(optionsPayload, "registerButtonId"));
        Assert.Equal("confirmMoveButton", ReadStringProperty(optionsPayload, "confirmButtonId"));
        Assert.Equal("undoLastButton", ReadStringProperty(optionsPayload, "undoButtonId"));
        Assert.Equal("clearCancelButton", ReadStringProperty(optionsPayload, "clearCancelButtonId"));
    }

    [Fact]
    public async Task FocusAsync_InvokesFocusWithElementId()
    {
        var js = new CapturingJsRuntime();
        var client = new LagerScannerClient(js);

        await client.FocusAsync("productInput");

        var call = Assert.Single(js.Invocations);
        Assert.Equal("lagerScanner.focus", call.Identifier);
        Assert.Equal(new object?[] { "productInput" }, call.Arguments);
    }

    [Fact]
    public async Task OpenInNewTabAsync_InvokesWindowOpenWithBlankTarget()
    {
        var js = new CapturingJsRuntime();
        var client = new LagerScannerClient(js);

        await client.OpenInNewTabAsync("https://example.local");

        var call = Assert.Single(js.Invocations);
        Assert.Equal("open", call.Identifier);
        Assert.Equal(new object?[] { "https://example.local", "_blank" }, call.Arguments);
    }

    [Fact]
    public async Task GetInputValueAsync_InvokesGetValueWithElementId()
    {
        var js = new CapturingJsRuntime();
        var client = new LagerScannerClient(js);

        _ = await client.GetInputValueAsync("palletScanInput");

        var call = Assert.Single(js.Invocations);
        Assert.Equal("lagerScanner.getValue", call.Identifier);
        Assert.Equal(new object?[] { "palletScanInput" }, call.Arguments);
    }

    private static string? ReadStringProperty(object? value, string propertyName)
    {
        var property = value?.GetType().GetProperty(propertyName);
        return property?.GetValue(value) as string;
    }

    private sealed class CapturingJsRuntime : IJSRuntime
    {
        public List<Invocation> Invocations { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(new Invocation(identifier, args ?? []));
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Invocations.Add(new Invocation(identifier, args ?? []));
            return ValueTask.FromResult(default(TValue)!);
        }
    }

    private sealed record Invocation(string Identifier, object?[] Arguments);
}
