using LagerPalleSortering.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using LagerPalleSortering.Services;

namespace LagerPalleSortering.Components.Pages;

public partial class Home
{
    private const string ProductInputId = "productInput";
    private const string ConfirmCountInputId = "confirmCountInput";
    private const string PalletScanInputId = "palletScanInput";
    private const string AlertSuccessCssClass = "alert-success";
    private const string AlertWarningCssClass = "alert-warning";
    private const string AlertErrorCssClass = "alert-danger";

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    [Inject]
    private ILagerScannerClient ScannerClient { get; set; } = default!;

    private readonly HomeFormModel registrationForm = new() { Quantity = 1 };
    private readonly List<PalletRecord> openPallets = new();
    private readonly List<ScanEntryRecord> entries = new();
    private string? statusMessage;
    private string statusAlertCssClass = AlertSuccessCssClass;
    private string scannedPalletCode = string.Empty;
    private string? lastSuggestedPalletId;
    private bool keepExpiryBetweenScans = true;
    private int confirmScanCount = 1;
    private bool isSimpleScannerMode;
    private bool showClearDatabaseWarning;
    private IBrowserFile? restoreFile;
    private string? selectedRestoreFileName;
    private bool shouldReinitializeScannerBindings = true;
    private bool focusProductAfterRender = true;

    protected override async Task OnInitializedAsync()
    {
        await ReloadDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _ = firstRender;

        if (shouldReinitializeScannerBindings)
        {
            // Rebind after mode switches so newly rendered inputs keep scanner flow.
            await ScannerClient.InitializeRegistrationFlowAsync(ProductInputId, "expiryInput", "quantityInput", "registerButton");
            await ScannerClient.InitializePalletConfirmAsync(PalletScanInputId, "confirmMoveButton", ConfirmCountInputId);
            await ScannerClient.InitializeHotkeysAsync(new LagerHotkeysOptions(
                ProductInputId,
                PalletScanInputId,
                "registerButton",
                "confirmMoveButton",
                "undoLastButton",
                "clearCancelButton"));
            shouldReinitializeScannerBindings = false;
        }

        if (focusProductAfterRender)
        {
            focusProductAfterRender = false;
            await FocusProductAsync();
        }
    }

    private void ToggleScannerMode()
    {
        isSimpleScannerMode = !isSimpleScannerMode;
        showClearDatabaseWarning = false;
        shouldReinitializeScannerBindings = true;
        focusProductAfterRender = true;
    }

    private async Task RegisterColliAsync()
    {
        var result = await DataService.RegisterColliAsync(registrationForm.ProductNumber ?? string.Empty, registrationForm.ExpiryDateRaw, registrationForm.Quantity);

        if (!result.Success)
        {
            SetStatus(result.Message, isError: true);
            await FocusProductAsync();
            return;
        }

        SetStatus(result.Message, isError: false);
        lastSuggestedPalletId = result.PalletId;
        registrationForm.ProductNumber = string.Empty;
        if (!keepExpiryBetweenScans)
        {
            registrationForm.ExpiryDateRaw = string.Empty;
        }

        registrationForm.Quantity = 1;
        await ReloadDataAsync();
        await FocusProductAsync();
    }

    private async Task ConfirmMoveAsync()
    {
        if (confirmScanCount <= 0)
        {
            SetErrorStatus("Antal at bekræfte skal være større end 0.");
            await FocusAsync(ConfirmCountInputId);
            return;
        }

        var confirmed = 0;
        string lastMessage = string.Empty;
        string? palletId = null;

        // Confirm one physical colli per iteration.
        for (var i = 0; i < confirmScanCount; i++)
        {
            var result = await DataService.ConfirmMoveByPalletScanAsync(scannedPalletCode, bypassDuplicateGuard: i > 0);
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
            SetSuccessStatus($"Flytning bekræftet: {confirmed} kolli på {palletId}.");
            scannedPalletCode = string.Empty;
            await ReloadDataAsync();
        }
        else if (confirmed > 0)
        {
            SetWarningStatus($"Delvis bekræftelse: {confirmed}/{confirmScanCount}. {lastMessage}");
            await ReloadDataAsync();
        }
        else
        {
            SetErrorStatus(lastMessage);
        }

        await FocusAsync(PalletScanInputId);
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
        await ScannerClient.OpenInNewTabAsync(url);
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
        ResetRegistrationForm();

        await ReloadDataAsync();
        SetStatus("Databasen er ryddet.", isError: false);
        await FocusProductAsync();
    }

    private void OnRestoreFileSelected(InputFileChangeEventArgs args)
    {
        restoreFile = args.File;
        selectedRestoreFileName = restoreFile?.Name;
    }

    private bool CanRestoreDatabase => restoreFile is not null;

    private async Task RestoreDatabaseAsync()
    {
        if (restoreFile is null)
        {
            SetStatus("Vælg en backupfil først.", isError: true);
            return;
        }

        try
        {
            await using var stream = restoreFile.OpenReadStream(maxAllowedSize: 128 * 1024 * 1024);
            await DataService.RestoreDatabaseAsync(stream);
            restoreFile = null;
            selectedRestoreFileName = null;
            await ReloadDataAsync();
            SetStatus("Database gendannet fra backup.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus($"Gendan fejlede: {ex.Message}", isError: true);
        }
    }

    private async Task ReloadDataAsync()
    {
        // Reload both sections from source-of-truth after each mutation.
        openPallets.Clear();
        openPallets.AddRange(await DataService.GetOpenPalletsAsync());

        entries.Clear();
        entries.AddRange(await DataService.GetRecentEntriesAsync(WarehouseConstants.DefaultRecentEntries));
    }

    private void SetStatus(string message, bool isError)
    {
        statusMessage = message;
        statusAlertCssClass = isError ? AlertErrorCssClass : AlertSuccessCssClass;
    }

    private void SetSuccessStatus(string message)
    {
        statusMessage = message;
        statusAlertCssClass = AlertSuccessCssClass;
    }

    private void SetWarningStatus(string message)
    {
        statusMessage = message;
        statusAlertCssClass = AlertWarningCssClass;
    }

    private void SetErrorStatus(string message)
    {
        statusMessage = message;
        statusAlertCssClass = AlertErrorCssClass;
    }

    private void ResetRegistrationForm()
    {
        registrationForm.ProductNumber = string.Empty;
        registrationForm.Quantity = 1;
        if (!keepExpiryBetweenScans)
        {
            registrationForm.ExpiryDateRaw = string.Empty;
        }
    }

    private async Task FocusAsync(string inputId)
    {
        await ScannerClient.FocusAsync(inputId);
    }

    private async Task FocusProductAsync()
    {
        await FocusAsync(ProductInputId);
    }
}
