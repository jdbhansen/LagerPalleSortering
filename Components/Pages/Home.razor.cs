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
    private readonly HomeConfirmationModel confirmationForm = new();
    private readonly HomeScannerPreferencesModel scannerPreferences = new();
    private readonly List<PalletRecord> openPallets = new();
    private readonly List<ScanEntryRecord> entries = new();
    private string? statusMessage;
    private string statusAlertCssClass = AlertSuccessCssClass;
    private string? lastSuggestedPalletId;
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

    private async Task RegisterColliAsync(RegisterColliInput input)
    {
        registrationForm.ProductNumber = input.ProductNumber;
        registrationForm.ExpiryDateRaw = input.ExpiryDateRaw;
        registrationForm.Quantity = input.Quantity;

        var result = await DataService.RegisterColliAsync(
            input.ProductNumber ?? string.Empty,
            input.ExpiryDateRaw,
            input.Quantity);

        if (!result.Success)
        {
            SetStatus(result.Message, isError: true);
            await FocusProductAsync();
            return;
        }

        SetStatus(result.Message, isError: false);
        lastSuggestedPalletId = result.PalletId;
        registrationForm.ProductNumber = string.Empty;
        if (!scannerPreferences.KeepExpiryBetweenScans)
        {
            registrationForm.ExpiryDateRaw = string.Empty;
        }

        registrationForm.Quantity = 1;
        await ReloadDataAsync();
        await FocusProductAsync();
    }

    private async Task ConfirmMoveAsync(MoveConfirmInput input)
    {
        var scannedCode = string.IsNullOrWhiteSpace(input.ScannedPalletCode)
            ? await ScannerClient.GetInputValueAsync(PalletScanInputId)
            : input.ScannedPalletCode;

        var scanCount = input.ConfirmScanCount;
        if (scanCount <= 0)
        {
            var rawCount = await ScannerClient.GetInputValueAsync(ConfirmCountInputId);
            if (!int.TryParse(rawCount, out scanCount))
            {
                scanCount = input.ConfirmScanCount;
            }
        }

        confirmationForm.ScannedPalletCode = scannedCode ?? string.Empty;
        confirmationForm.ConfirmScanCount = scanCount;

        if (string.IsNullOrWhiteSpace(confirmationForm.ScannedPalletCode) && !string.IsNullOrWhiteSpace(lastSuggestedPalletId))
        {
            // Fallback for scanner/input sync edge-cases: use latest suggested pallet.
            confirmationForm.ScannedPalletCode = $"PALLET:{lastSuggestedPalletId}";
        }

        if (confirmationForm.ConfirmScanCount <= 0)
        {
            SetErrorStatus("Antal at bekræfte skal være større end 0.");
            await FocusAsync(ConfirmCountInputId);
            return;
        }

        var batchResult = await DataService.ConfirmMoveBatchByPalletScanAsync(
            confirmationForm.ScannedPalletCode,
            confirmationForm.ConfirmScanCount);

        if (batchResult.Status == "success")
        {
            SetSuccessStatus(batchResult.Message);
            confirmationForm.ScannedPalletCode = string.Empty;
            await ReloadDataAsync();
        }
        else if (batchResult.Status == "warning")
        {
            SetWarningStatus(batchResult.Message);
            await ReloadDataAsync();
        }
        else
        {
            SetErrorStatus(batchResult.Message);
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
        confirmationForm.ScannedPalletCode = string.Empty;
        confirmationForm.ConfirmScanCount = 1;
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
        if (!scannerPreferences.KeepExpiryBetweenScans)
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
