using ClosedXML.Excel;
using Milo.Application.Common.Interfaces;

namespace Milo.Infraestructure.Services;

public sealed class ClosedXmlExcelReportService : IExcelReportService
{
    private static readonly string[] Headers =
        ["Inmueble", "Ciudad", "Check-in", "Check-out", "Noches", "Precio Total", "Huésped", "Email"];

    public Task<byte[]> GenerateReservationsReportAsync(
        IEnumerable<ReservationReportRow> rows, string reportTitle)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Reservaciones");

        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2D6A4F");
        }

        var rowList = rows.ToList();
        for (var i = 0; i < rowList.Count; i++)
        {
            var row = rowList[i];
            var rowIndex = i + 2;
            var bgColor = i % 2 == 0 ? XLColor.White : XLColor.FromHtml("#F2F2F2");

            sheet.Cell(rowIndex, 1).Value = row.PropertyName;
            sheet.Cell(rowIndex, 2).Value = row.PropertyCity;
            sheet.Cell(rowIndex, 3).Value = row.CheckInDate.ToString("dd/MM/yyyy");
            sheet.Cell(rowIndex, 4).Value = row.CheckOutDate.ToString("dd/MM/yyyy");
            sheet.Cell(rowIndex, 5).Value = row.TotalNights;
            sheet.Cell(rowIndex, 6).Value = (double)row.TotalPrice;
            sheet.Cell(rowIndex, 6).Style.NumberFormat.Format = "#,##0.00";
            sheet.Cell(rowIndex, 7).Value = $"{row.GuestFirstName} {row.GuestLastName}";
            sheet.Cell(rowIndex, 8).Value = row.GuestEmail;

            for (var col = 1; col <= Headers.Length; col++)
                sheet.Cell(rowIndex, col).Style.Fill.BackgroundColor = bgColor;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return Task.FromResult(stream.ToArray());
    }
}
