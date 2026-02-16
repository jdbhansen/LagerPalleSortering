using System.ComponentModel.DataAnnotations;

namespace LagerPalleSortering.Components.Pages;

public sealed class HomeFormModel
{
    [Required(ErrorMessage = "Varenummer er påkrævet.")]
    public string? ProductNumber { get; set; }

    // UI validation guards basic shape; service layer performs strict date validation.
    [RegularExpression(@"^$|^\d{8}$", ErrorMessage = "Holdbarhed skal være tom eller i format YYYYMMDD.")]
    public string? ExpiryDateRaw { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Antal kolli skal være større end 0.")]
    public int Quantity { get; set; } = 1;
}
