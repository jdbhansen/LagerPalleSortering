using Microsoft.JSInterop;

namespace LagerPalleSortering.Services;

public sealed class LagerScannerClient(IJSRuntime jsRuntime) : ILagerScannerClient
{
    public Task InitializeRegistrationFlowAsync(string productInputId, string expiryInputId, string quantityInputId, string registerButtonId) =>
        jsRuntime.InvokeVoidAsync("lagerScanner.init", productInputId, expiryInputId, quantityInputId, registerButtonId).AsTask();

    public Task InitializePalletConfirmAsync(string palletInputId, string confirmButtonId, string confirmCountInputId) =>
        jsRuntime.InvokeVoidAsync("lagerScanner.initPalletConfirm", palletInputId, confirmButtonId, confirmCountInputId).AsTask();

    public Task InitializeHotkeysAsync(LagerHotkeysOptions options) =>
        jsRuntime.InvokeVoidAsync("lagerScanner.initHotkeys", new
        {
            productInputId = options.ProductInputId,
            palletInputId = options.PalletInputId,
            registerButtonId = options.RegisterButtonId,
            confirmButtonId = options.ConfirmButtonId,
            undoButtonId = options.UndoButtonId,
            clearCancelButtonId = options.ClearCancelButtonId
        }).AsTask();

    public Task FocusAsync(string elementId) =>
        jsRuntime.InvokeVoidAsync("lagerScanner.focus", elementId).AsTask();

    public Task OpenInNewTabAsync(string url) =>
        jsRuntime.InvokeVoidAsync("open", url, "_blank").AsTask();
}
