namespace LagerPalleSortering.Services;

public interface ILagerScannerClient
{
    Task InitializeRegistrationFlowAsync(string productInputId, string expiryInputId, string quantityInputId, string registerButtonId);
    Task InitializePalletConfirmAsync(string palletInputId, string confirmButtonId, string confirmCountInputId);
    Task InitializeHotkeysAsync(LagerHotkeysOptions options);
    Task FocusAsync(string elementId);
    Task<string> GetInputValueAsync(string elementId);
    Task OpenInNewTabAsync(string url);
}
