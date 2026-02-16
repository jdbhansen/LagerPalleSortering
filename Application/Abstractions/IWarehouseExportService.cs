namespace LagerPalleSortering.Application.Abstractions;

/// <summary>
/// Export contract for operational warehouse data.
/// </summary>
public interface IWarehouseExportService
{
    /// <summary>
    /// Exports scan entries as UTF-8 CSV with header.
    /// </summary>
    Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Exports open pallets and scan entries as an Excel workbook.
    /// </summary>
    Task<byte[]> ExportExcelAsync(CancellationToken cancellationToken = default);
}
