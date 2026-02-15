using LagerPalleSortering.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LagerPalleSortering.Components.Pages;

public partial class Home
{
    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private readonly HomeFormModel form = new() { Quantity = 1 };
    private readonly List<PalletRecord> openPallets = new();
    private readonly List<ScanEntryRecord> entries = new();
    private string? statusMessage;
    private string statusCss = "alert-info";
    private string scannedPalletCode = string.Empty;
    private string? lastSuggestedPalletId;
    private bool keepExpiryBetweenScans = true;
    private int confirmScanCount = 1;
    private bool showClearDatabaseWarning;

    protected override async Task OnInitializedAsync()
    {
        await ReloadDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        await JS.InvokeVoidAsync("lagerScanner.init", "productInput", "expiryInput", "quantityInput", "registerButton");
        await JS.InvokeVoidAsync("lagerScanner.initPalletConfirm", "palletScanInput", "confirmMoveButton", "confirmCountInput");
        await JS.InvokeVoidAsync("lagerScanner.initHotkeys", new
        {
            productInputId = "productInput",
            palletInputId = "palletScanInput",
            registerButtonId = "registerButton",
            confirmButtonId = "confirmMoveButton",
            undoButtonId = "undoLastButton",
            clearCancelButtonId = "clearCancelButton"
        });
        await FocusProductAsync();
    }

    private async Task RegisterColliAsync()
    {
        var result = await DataService.RegisterColliAsync(form.ProductNumber ?? string.Empty, form.ExpiryDateRaw, form.Quantity);

        if (!result.Success)
        {
            SetStatus(result.Message, isError: true);
            await FocusProductAsync();
            return;
        }

        SetStatus(result.Message, isError: false);
        lastSuggestedPalletId = result.PalletId;
        form.ProductNumber = string.Empty;
        if (!keepExpiryBetweenScans)
        {
            form.ExpiryDateRaw = string.Empty;
        }

        form.Quantity = 1;
        await ReloadDataAsync();
        await FocusProductAsync();
    }

    private async Task ConfirmMoveAsync()
    {
        if (confirmScanCount <= 0)
        {
            statusMessage = "Antal at bekræfte skal være større end 0.";
            statusCss = "alert-danger";
            await JS.InvokeVoidAsync("lagerScanner.focus", "confirmCountInput");
            return;
        }

        var confirmed = 0;
        string lastMessage = string.Empty;
        string? palletId = null;

        for (var i = 0; i < confirmScanCount; i++)
        {
            var result = await DataService.ConfirmMoveByPalletScanAsync(scannedPalletCode);
            lastMessage = result.Message;
            palletId = result.PalletId;
            if (!result.Success)
            {
                break;
            }

            confirmed++;
        }

        if (confirmed == confirmScanCount)
        {
            statusMessage = $"Flytning bekræftet: {confirmed} kolli på {palletId}.";
            statusCss = "alert-success";
            scannedPalletCode = string.Empty;
            await ReloadDataAsync();
        }
        else if (confirmed > 0)
        {
            statusMessage = $"Delvis bekræftelse: {confirmed}/{confirmScanCount}. {lastMessage}";
            statusCss = "alert-warning";
            await ReloadDataAsync();
        }
        else
        {
            statusMessage = lastMessage;
            statusCss = "alert-danger";
        }

        await JS.InvokeVoidAsync("lagerScanner.focus", "palletScanInput");
    }

    private async Task ClosePalletAsync(string palletId)
    {
        await DataService.ClosePalletAsync(palletId);
        SetStatus($"Palle {palletId} er lukket.", isError: false);
        await ReloadDataAsync();
        await FocusProductAsync();
    }

    private async Task CloseAndPrintContentsAsync(string palletId)
    {
        await DataService.ClosePalletAsync(palletId);
        SetStatus($"Palle {palletId} er lukket. Printer indholdsliste.", isError: false);
        await ReloadDataAsync();

        var url = Navigation.ToAbsoluteUri($"/print-pallet-contents/{palletId}").ToString();
        await JS.InvokeVoidAsync("open", url, "_blank");
        await FocusProductAsync();
    }

    private async Task UndoLastAsync()
    {
        var undo = await DataService.UndoLastAsync();
        if (undo is null)
        {
            SetStatus("Der er intet at fortryde.", isError: true);
            await FocusProductAsync();
            return;
        }

        SetStatus($"Fortrudt: {undo.Quantity} kolli fjernet fra {undo.PalletId}.", isError: false);
        await ReloadDataAsync();
        await FocusProductAsync();
    }

    private void ShowClearDatabaseWarning()
    {
        showClearDatabaseWarning = true;
        SetStatus("Bekræft at databasen skal ryddes.", isError: true);
    }

    private void CancelClearDatabase()
    {
        showClearDatabaseWarning = false;
        SetStatus("Ryd database annulleret.", isError: false);
    }

    private async Task ClearDatabaseAsync()
    {
        await DataService.ClearAllDataAsync();
        showClearDatabaseWarning = false;
        lastSuggestedPalletId = null;
        scannedPalletCode = string.Empty;
        confirmScanCount = 1;
        form.ProductNumber = string.Empty;
        form.Quantity = 1;
        if (!keepExpiryBetweenScans)
        {
            form.ExpiryDateRaw = string.Empty;
        }

        await ReloadDataAsync();
        SetStatus("Databasen er ryddet.", isError: false);
        await FocusProductAsync();
    }

    private async Task ReloadDataAsync()
    {
        openPallets.Clear();
        openPallets.AddRange(await DataService.GetOpenPalletsAsync());

        entries.Clear();
        entries.AddRange(await DataService.GetRecentEntriesAsync(WarehouseConstants.DefaultRecentEntries));
    }

    private void SetStatus(string message, bool isError)
    {
        statusMessage = message;
        statusCss = isError ? "alert-danger" : "alert-success";
    }

    private async Task FocusProductAsync()
    {
        await JS.InvokeVoidAsync("lagerScanner.focus", "productInput");
    }
}
