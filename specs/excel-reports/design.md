# Feature: excel-reports — Design

## Nuevos paquetes NuGet

En `Milo.Infraestructure.csproj`:
```xml
<PackageReference Include="ClosedXML" Version="0.*" />
```

## Sin migration
Feature de solo lectura sobre datos existentes.

---

## Estructura de carpetas nueva

```
src/
├── Milo.Domain/
│   └── Repositories/
│       └── IReservationRepository.cs          ← 1 método nuevo
│
├── Milo.Application/
│   ├── Common/
│   │   └── Interfaces/
│   │       └── IExcelReportService.cs          ← nueva (con ReservationReportRow)
│   └── Reports/
│       └── Queries/
│           └── GenerateReservationsReport/
│               ├── GenerateReservationsReportQuery.cs
│               └── GenerateReservationsReportHandler.cs
│
├── Milo.Infraestructure/
│   ├── Persistence/
│   │   └── Repositories/
│   │       └── ReservationRepository.cs        ← 1 método nuevo
│   └── Services/
│       └── ClosedXmlExcelReportService.cs      ← nueva
│
└── Milo.Api/
    └── Controllers/
        └── ReportsController.cs               ← nueva
```

**Archivos modificados:**
- `Milo.Infraestructure/Milo.Infraestructure.csproj` — agregar ClosedXML
- `Milo.Infraestructure/DependencyInjection.cs` — registrar `IExcelReportService`

---

## Domain — IReservationRepository (modificación)

Agregar un método especializado para el reporte (parámetros opcionales, con navegaciones):

```csharp
Task<IReadOnlyList<Reservation>> GetConfirmedForReportAsync(
    Guid ownerId,
    Guid? propertyId,
    DateOnly? from,
    DateOnly? to,
    CancellationToken cancellationToken = default);
```

No se reutilizan `GetConfirmedByOwnerAsync` ni `GetConfirmedByPropertyAsync` porque:
1. Ambos requieren fechas no nullables.
2. Ninguno incluye las navegaciones `Property` y `Guest` necesarias para mapear email y ciudad.

---

## Application — IExcelReportService + ReservationReportRow

### `IExcelReportService.cs`

```csharp
namespace Milo.Application.Common.Interfaces;

public interface IExcelReportService
{
    Task<byte[]> GenerateReservationsReportAsync(
        IEnumerable<ReservationReportRow> rows, string reportTitle);
}

public record ReservationReportRow(
    string PropertyName,
    string PropertyCity,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int TotalNights,
    decimal TotalPrice,
    string GuestFirstName,
    string GuestLastName,
    string GuestEmail);
```

---

## Application — GenerateReservationsReport

### `GenerateReservationsReportQuery.cs`

```csharp
public record GenerateReservationsReportQuery(
    Guid? PropertyId,
    DateOnly? DateFrom,
    DateOnly? DateTo) : IRequest<Result<byte[]>>;
```

### `GenerateReservationsReportHandler.cs`

Inyecta: `IPropertyRepository, IReservationRepository, IExcelReportService, ICurrentUserProvider`

```
1. ownerId = currentUser.UserId!.Value
2. Si PropertyId.HasValue:
       property = await propertyRepository.GetByIdAsync(PropertyId.Value, ct)
       if null || property.OwnerId != ownerId → Result.Failure("Inmueble no encontrado")  [404]
3. reservations = await reservationRepository.GetConfirmedForReportAsync(
       ownerId, PropertyId, DateFrom, DateTo, ct)
4. rows = reservations.Select(r => new ReservationReportRow(
       r.Property.Name, r.Property.City,
       r.CheckInDate, r.CheckOutDate,
       r.TotalNights, r.TotalPrice,
       r.Guest.FirstName, r.Guest.LastName, r.Guest.Email))
5. bytes = await excelReportService.GenerateReservationsReportAsync(rows, "Reporte de Reservaciones")
6. Result.Success(bytes)
```

Sin filtro de fecha especial en el handler — la lógica vive íntegramente en `GetConfirmedForReportAsync`.
Si `rows` está vacío: el servicio genera el Excel igualmente con solo encabezados.

---

## Infrastructure — GetConfirmedForReportAsync

### En `ReservationRepository.cs`

```csharp
public async Task<IReadOnlyList<Reservation>> GetConfirmedForReportAsync(
    Guid ownerId, Guid? propertyId, DateOnly? from, DateOnly? to,
    CancellationToken cancellationToken = default)
{
    var query = dbContext.Reservations
        .Include(r => r.Property)
        .Include(r => r.Guest)
        .Where(r => r.Property.OwnerId == ownerId
                 && r.Status == ReservationStatus.Confirmed);

    if (propertyId.HasValue)
        query = query.Where(r => r.PropertyId == propertyId.Value);
    if (from.HasValue)
        query = query.Where(r => r.CheckInDate >= from.Value);
    if (to.HasValue)
        query = query.Where(r => r.CheckInDate <= to.Value);

    return await query.OrderBy(r => r.CheckInDate).ToListAsync(cancellationToken);
}
```

Construcción incremental del query: EF Core traduce el `IQueryable<Reservation>` completo a
una sola SQL con los WHERE opcionales aplicados antes de ejecutar.

---

## Infrastructure — ClosedXmlExcelReportService

### `ClosedXmlExcelReportService.cs`

```csharp
public sealed class ClosedXmlExcelReportService : IExcelReportService
{
    private static readonly string[] Headers =
        ["Inmueble", "Ciudad", "Check-in", "Check-out", "Noches", "Precio Total", "Huésped", "Email"];

    public Task<byte[]> GenerateReservationsReportAsync(
        IEnumerable<ReservationReportRow> rows, string reportTitle)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Reservaciones");

        // Encabezados
        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2D6A4F");
        }

        // Filas de datos
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
```

**Nota sobre `decimal` → `double`:** ClosedXML solo acepta `double` para valores numéricos en
celdas (`.Value = (double)row.TotalPrice`); el formato numérico `#,##0.00` controla la presentación.

El método es síncrono internamente (ClosedXML no tiene API async); `Task.FromResult` cumple
el contrato de la interfaz sin costos adicionales.

---

## Infrastructure — DependencyInjection (modificación)

```csharp
services.AddScoped<IExcelReportService, ClosedXmlExcelReportService>();
```

---

## API — ReportsController

```csharp
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Owner")]
public sealed class ReportsController(ISender sender) : ControllerBase
{
    [HttpGet("reservations")]
    public async Task<IActionResult> DownloadReservations(
        [FromQuery] Guid? propertyId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new GenerateReservationsReportQuery(propertyId, dateFrom, dateTo), ct);

        if (!result.IsSuccess)
            return Problem(title: result.Error!, statusCode: StatusCodes.Status404NotFound);

        var fileName = $"reporte-reservaciones-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(result.Value,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}
```

El controller devuelve `File(...)` directamente — no un DTO. El navegador descarga el archivo.
`result.Error!` con null-forgiving operator (solo se llama cuando `!result.IsSuccess`).

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| Método `GetConfirmedForReportAsync` separado de los existentes | Los métodos existentes no cargan navegaciones (Property, Guest) y requieren fechas no-nullables; un método especializado es más limpio que sobrecargar |
| Construcción incremental del `IQueryable` | EF Core aplaza la ejecución hasta `ToListAsync`; todos los WHERE opcionales se convierten en una sola SQL |
| `Task.FromResult` en ClosedXML | La librería es síncrona; no tiene sentido añadir `Task.Run` para fingir asincronía — `Task.FromResult` es idiomático para implementaciones síncronas de interfaces async |
| `decimal → double` para ClosedXML | ClosedXML no acepta `decimal` en `.Value`; la conversión es segura para valores monetarios en este rango |
| Fechas como strings (`dd/MM/yyyy`) | ClosedXML puede almacenar `DateOnly` pero genera formato ISO; convertir a string garantiza el formato solicitado en todas las versiones |
| Vacío = solo encabezados | El handler no distingue "sin resultados" como error — retorna bytes igualmente; el Owner puede guardar el archivo aunque esté vacío |
| `ClosedXML 0.*` (wildcard minor) | Toma cualquier parche o minor dentro de la versión 0.x estable; pinned a 0.x por compatibilidad con la API usada en el proyecto |
