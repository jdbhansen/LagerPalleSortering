namespace LagerPalleSortering.Application.Abstractions;

public interface IWarehouseExportService
{
    Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default);
    Task<byte[]> ExportExcelAsync(CancellationToken cancellationToken = default);
}
