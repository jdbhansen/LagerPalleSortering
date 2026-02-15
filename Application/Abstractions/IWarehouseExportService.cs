namespace LagerPalleSortering.Application.Abstractions;

public interface IWarehouseExportService
{
    Task<byte[]> ExportCsvAsync();
    Task<byte[]> ExportExcelAsync();
}
