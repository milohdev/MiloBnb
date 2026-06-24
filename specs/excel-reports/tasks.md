# Feature: excel-reports — Tasks

> Implementar en este orden. `dotnet build --no-incremental --nologo` después de cada tarea.
> No hay migration. Firmas de referencia en `design.md`.

---

## T-01 — Domain: GetConfirmedForReportAsync en IReservationRepository

**Archivo a modificar:**
- `src/Milo.Domain/Repositories/IReservationRepository.cs`

Agregar al final de la interfaz (antes del `SaveChangesAsync`):

```csharp
Task<IReadOnlyList<Reservation>> GetConfirmedForReportAsync(
    Guid ownerId,
    Guid? propertyId,
    DateOnly? from,
    DateOnly? to,
    CancellationToken cancellationToken = default);
```

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-02 — Application: IExcelReportService + ReservationReportRow

**Archivo a crear:**
- `src/Milo.Application/Common/Interfaces/IExcelReportService.cs`

Contiene la interfaz y el record en el mismo archivo:

```csharp
namespace Milo.Application.Common.Interfaces;

public interface IExcelReportService
{
    Task<byte[]> GenerateReservationsReportAsync(
        IEnumerable<ReservationReportRow> rows, string reportTitle);
}

public record ReservationReportRow(
    string PropertyName, string PropertyCity,
    DateOnly CheckInDate, DateOnly CheckOutDate,
    int TotalNights, decimal TotalPrice,
    string GuestFirstName, string GuestLastName, string GuestEmail);
```

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-03 — Application: GenerateReservationsReportQuery + Handler

**Archivos a crear:**
- `src/Milo.Application/Reports/Queries/GenerateReservationsReport/GenerateReservationsReportQuery.cs`
- `src/Milo.Application/Reports/Queries/GenerateReservationsReport/GenerateReservationsReportHandler.cs`

`GenerateReservationsReportQuery(Guid? PropertyId, DateOnly? DateFrom, DateOnly? DateTo)` → `Result<byte[]>`.

Handler inyecta `IPropertyRepository, IReservationRepository, IExcelReportService, ICurrentUserProvider`:

1. `ownerId = currentUser.UserId!.Value`
2. Si `PropertyId.HasValue`:
   - `GetByIdAsync(PropertyId.Value, ct)` → `null || property.OwnerId != ownerId` → `Result.Failure("Inmueble no encontrado")`
3. `GetConfirmedForReportAsync(ownerId, PropertyId, DateFrom, DateTo, ct)` → reservations
4. `rows = reservations.Select(r => new ReservationReportRow(r.Property.Name, r.Property.City, r.CheckInDate, r.CheckOutDate, r.TotalNights, r.TotalPrice, r.Guest.FirstName, r.Guest.LastName, r.Guest.Email))`
5. `bytes = await excelReportService.GenerateReservationsReportAsync(rows, "Reporte de Reservaciones")`
6. `Result.Success(bytes)`

Sin validación adicional de fechas — la lógica de filtrado vive en el repositorio.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-04 — Infrastructure: ClosedXML + ReservationRepository + ClosedXmlExcelReportService + DI

**Paquete a agregar:**

```bash
dotnet add src/Milo.Infraestructure/Milo.Infraestructure.csproj package ClosedXML
```

**Archivos a crear:**
- `src/Milo.Infraestructure/Services/ClosedXmlExcelReportService.cs`

**Archivos a modificar:**
- `src/Milo.Infraestructure/Persistence/Repositories/ReservationRepository.cs` — método nuevo
- `src/Milo.Infraestructure/DependencyInjection.cs` — `AddScoped<IExcelReportService, ClosedXmlExcelReportService>()`

`GetConfirmedForReportAsync` en `ReservationRepository`:
```csharp
var query = dbContext.Reservations
    .Include(r => r.Property)
    .Include(r => r.Guest)
    .Where(r => r.Property.OwnerId == ownerId
             && r.Status == ReservationStatus.Confirmed);

if (propertyId.HasValue) query = query.Where(r => r.PropertyId == propertyId.Value);
if (from.HasValue)       query = query.Where(r => r.CheckInDate >= from.Value);
if (to.HasValue)         query = query.Where(r => r.CheckInDate <= to.Value);

return await query.OrderBy(r => r.CheckInDate).ToListAsync(cancellationToken);
```

`ClosedXmlExcelReportService`:
- `using ClosedXML.Excel;`
- Headers array: `["Inmueble", "Ciudad", "Check-in", "Check-out", "Noches", "Precio Total", "Huésped", "Email"]`
- Fila 1: bold, `XLColor.White`, fondo `XLColor.FromHtml("#2D6A4F")`
- Filas 2+: fondo alternado — `XLColor.White` (i par) / `XLColor.FromHtml("#F2F2F2")` (i impar)
  donde `i` es el índice base-0 de la fila de datos
- Fechas: `.ToString("dd/MM/yyyy")` (string, no DateOnly nativo)
- TotalPrice: `(double)row.TotalPrice` en `.Value`, más `.Style.NumberFormat.Format = "#,##0.00"`
- Huésped: `$"{row.GuestFirstName} {row.GuestLastName}"` (combinado en columna G)
- `sheet.Columns().AdjustToContents()` al final
- `workbook.SaveAs(stream)` → `Task.FromResult(stream.ToArray())`

**Checkpoint:** `dotnet build src/Milo.Infraestructure/Milo.Infraestructure.csproj --no-incremental` sin errores.

---

## T-05 — API: ReportsController

**Archivo a crear:**
- `src/Milo.Api/Controllers/ReportsController.cs`

`[Authorize(Roles = "Owner")]` a nivel de clase.

`GET /api/reports/reservations`:
```csharp
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
```

**Checkpoint:** `dotnet build --no-incremental --nologo` en la raíz sin errores.

---

## Verificación final

- [ ] `dotnet build` raíz: 0 errores, 0 warnings
- [ ] Swagger muestra `GET /api/reports/reservations`
- [ ] Sin token → 401; con Guest → 403
- [ ] Sin reservas → 200 con archivo .xlsx descargable (solo encabezados)
- [ ] Con reservas → 200 con archivo .xlsx con datos y filas alternadas
- [ ] Con `propertyId` ajeno → 404
- [ ] Con `propertyId` propio + fechas → reporte filtrado
- [ ] Archivo abre en Excel/LibreOffice sin errores: encabezados verdes, texto blanco, negrita
- [ ] Precio Total con 2 decimales, fechas en dd/MM/yyyy
- [ ] Columnas con ancho automático visible en Excel
- [ ] Actualizar `progress/current.md`
