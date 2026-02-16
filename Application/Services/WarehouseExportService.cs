using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using LagerPalleSortering.Application.Abstractions;
using LagerPalleSortering.Domain;

namespace LagerPalleSortering.Application.Services;

/// <summary>
/// Creates operational exports from repository snapshots.
/// </summary>
public sealed class WarehouseExportService : IWarehouseExportService
{
    private const string ExportTimestampFormat = "yyyy-MM-dd HH:mm:ss";

    private readonly IWarehouseRepository _repository;

    public WarehouseExportService(IWarehouseRepository repository)
    {
        _repository = repository;
    }

    public async Task<byte[]> ExportCsvAsync(CancellationToken cancellationToken = default)
    {
        // Exports are ordered oldest->newest for better readability in spreadsheets.
        var entries = await _repository.GetRecentEntriesAsync(WarehouseConstants.MaxExportRows, cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("TimestampUtc,PalletId,ProductNumber,ExpiryDate,Quantity,ConfirmedQuantity,CreatedNewPallet,ConfirmedMoved,ConfirmedAtUtc");

        foreach (var entry in entries.OrderBy(e => e.Timestamp))
        {
            sb
                .Append(EscapeCsv(FormatTimestamp(entry.Timestamp))).Append(',')
                .Append(EscapeCsv(entry.PalletId)).Append(',')
                .Append(EscapeCsv(entry.ProductNumber)).Append(',')
                .Append(EscapeCsv(entry.ExpiryDate)).Append(',')
                .Append(entry.Quantity.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(entry.ConfirmedQuantity.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(entry.CreatedNewPallet ? "1" : "0").Append(',')
                .Append(entry.ConfirmedMoved ? "1" : "0").Append(',')
                .Append(EscapeCsv(FormatOptionalTimestamp(entry.ConfirmedAt)))
                .AppendLine();
        }

        // BOM is included to ensure Danish characters render correctly in Excel.
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportExcelAsync(CancellationToken cancellationToken = default)
    {
        var openPallets = await _repository.GetOpenPalletsAsync(cancellationToken);
        var entries = await _repository.GetRecentEntriesAsync(WarehouseConstants.MaxExportRows, cancellationToken);

        using var wb = new XLWorkbook();
        var palletSheet = wb.Worksheets.Add("OpenPallets");
        palletSheet.Cell(1, 1).Value = "PalletId";
        palletSheet.Cell(1, 2).Value = "ProductNumber";
        palletSheet.Cell(1, 3).Value = "ExpiryDate";
        palletSheet.Cell(1, 4).Value = "TotalQuantity";
        palletSheet.Cell(1, 5).Value = "IsClosed";
        palletSheet.Cell(1, 6).Value = "CreatedAtUtc";

        var row = 2;
        foreach (var pallet in openPallets)
        {
            palletSheet.Cell(row, 1).Value = pallet.PalletId;
            palletSheet.Cell(row, 2).Value = pallet.ProductNumber;
            palletSheet.Cell(row, 3).Value = pallet.ExpiryDate;
            palletSheet.Cell(row, 4).Value = pallet.TotalQuantity;
            palletSheet.Cell(row, 5).Value = pallet.IsClosed ? 1 : 0;
            palletSheet.Cell(row, 6).Value = FormatTimestamp(pallet.CreatedAt);
            row++;
        }

        var entrySheet = wb.Worksheets.Add("ScanEntries");
        entrySheet.Cell(1, 1).Value = "TimestampUtc";
        entrySheet.Cell(1, 2).Value = "PalletId";
        entrySheet.Cell(1, 3).Value = "ProductNumber";
        entrySheet.Cell(1, 4).Value = "ExpiryDate";
        entrySheet.Cell(1, 5).Value = "Quantity";
        entrySheet.Cell(1, 6).Value = "ConfirmedQuantity";
        entrySheet.Cell(1, 7).Value = "CreatedNewPallet";
        entrySheet.Cell(1, 8).Value = "ConfirmedMoved";
        entrySheet.Cell(1, 9).Value = "ConfirmedAtUtc";

        row = 2;
        foreach (var entry in entries.OrderBy(e => e.Timestamp))
        {
            entrySheet.Cell(row, 1).Value = FormatTimestamp(entry.Timestamp);
            entrySheet.Cell(row, 2).Value = entry.PalletId;
            entrySheet.Cell(row, 3).Value = entry.ProductNumber;
            entrySheet.Cell(row, 4).Value = entry.ExpiryDate;
            entrySheet.Cell(row, 5).Value = entry.Quantity;
            entrySheet.Cell(row, 6).Value = entry.ConfirmedQuantity;
            entrySheet.Cell(row, 7).Value = entry.CreatedNewPallet ? 1 : 0;
            entrySheet.Cell(row, 8).Value = entry.ConfirmedMoved ? 1 : 0;
            entrySheet.Cell(row, 9).Value = FormatOptionalTimestamp(entry.ConfirmedAt);
            row++;
        }

        palletSheet.Columns().AdjustToContents();
        entrySheet.Columns().AdjustToContents();

        await using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static string EscapeCsv(string value)
    {
        // RFC4180-style escaping for comma, quote and line breaks.
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string FormatTimestamp(DateTime value) => value.ToString(ExportTimestampFormat, CultureInfo.InvariantCulture);

    private static string FormatOptionalTimestamp(DateTime? value) =>
        value.HasValue
            ? FormatTimestamp(value.Value)
            : string.Empty;
}
