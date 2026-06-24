# Feature: owner-dashboard — Tasks

> Implementar en este orden. `dotnet build --no-incremental --nologo` después de cada tarea.
> No hay migration — feature de solo lectura.
> Firmas de referencia en `design.md`.

---

## T-01 — Domain: nuevos métodos en IReservationRepository

**Archivo a modificar:**
- `src/Milo.Domain/Repositories/IReservationRepository.cs`

Agregar al final de la interfaz (antes del cierre `}`):

```csharp
Task<IReadOnlyList<Reservation>> GetConfirmedByOwnerAsync(
    Guid ownerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

Task<IReadOnlyList<Reservation>> GetConfirmedByPropertyAsync(
    Guid propertyId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
```

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-02 — Application: DTOs + GetOwnerDashboard

**Archivos a crear:**
- `src/Milo.Application/Dashboard/Queries/GetOwnerDashboard/OwnerDashboardDto.cs`
- `src/Milo.Application/Dashboard/Queries/GetOwnerDashboard/PropertySummaryDto.cs`
- `src/Milo.Application/Dashboard/Queries/GetOwnerDashboard/GetOwnerDashboardQuery.cs`
- `src/Milo.Application/Dashboard/Queries/GetOwnerDashboard/GetOwnerDashboardHandler.cs`

`OwnerDashboardDto`: `int TotalProperties, int TotalReservations, decimal TotalRevenue,
double OccupancyRate, decimal AveragePricePerNight, DateOnly DateFrom, DateOnly DateTo,
IReadOnlyList<PropertySummaryDto> ReservationsByProperty`.

`PropertySummaryDto`: `Guid PropertyId, string PropertyName, int ReservationCount, decimal Revenue`.

`GetOwnerDashboardQuery(DateOnly? DateFrom, DateOnly? DateTo)` → `Result<OwnerDashboardDto>`.

`GetOwnerDashboardHandler` inyecta `IPropertyRepository, IReservationRepository, ICurrentUserProvider`:

1. `ownerId = currentUser.UserId!.Value`
2. `(from, to) = ResolvePeriod(DateFrom, DateTo)`
3. `GetByOwnerIdAsync(ownerId)` → filtrar `.Where(p => p.IsActive).ToList()`
4. `GetConfirmedByOwnerAsync(ownerId, from, to)` → reservations
5. Métricas (ver design.md — OccupancyRate con guard `totalDays > 0 && TotalProperties > 0`)
6. `ReservationsByProperty` calculado en-memoria, `.OrderByDescending(x => x.Revenue)`
7. `Result.Success(dto)`

`ResolvePeriod` — helper privado estático:
```csharp
private static (DateOnly From, DateOnly To) ResolvePeriod(DateOnly? dateFrom, DateOnly? dateTo)
{
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    return (dateFrom, dateTo) switch
    {
        (null, null) => (today.AddDays(-30), today),
        ({ } from, null) => (from, today),
        (null, { } to) => (new DateOnly(2020, 1, 1), to),
        ({ } from, { } to) => (from, to)
    };
}
```

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.
(El build fallará por T-01 no implementado en Infrastructure — ignorar si solo falla Infrastructure, no Application.)

---

## T-03 — Application: PropertyMetricsDto + GetPropertyMetrics

**Archivos a crear:**
- `src/Milo.Application/Dashboard/Queries/GetPropertyMetrics/PropertyMetricsDto.cs`
- `src/Milo.Application/Dashboard/Queries/GetPropertyMetrics/GetPropertyMetricsQuery.cs`
- `src/Milo.Application/Dashboard/Queries/GetPropertyMetrics/GetPropertyMetricsHandler.cs`

`PropertyMetricsDto`: `Guid PropertyId, string PropertyName, decimal PricePerNight,
bool AllowSameDayBooking, int TotalReservations, decimal TotalRevenue, double OccupancyRate,
double AverageLengthOfStay, DateOnly DateFrom, DateOnly DateTo,
IReadOnlyList<ReservationDto> RecentReservations`.

Importar `ReservationDto` de `Milo.Application.Reservations.Queries.GetMyReservations`.

`GetPropertyMetricsQuery(Guid PropertyId, DateOnly? DateFrom, DateOnly? DateTo)` → `Result<PropertyMetricsDto>`.

`GetPropertyMetricsHandler` inyecta `IPropertyRepository, IReservationRepository, ICurrentUserProvider`:

1. `ownerId = currentUser.UserId!.Value`
2. `GetByIdAsync(PropertyId)` → `null || property.OwnerId != ownerId` → `Result.Failure("Inmueble no encontrado")`
3. `(from, to) = ResolvePeriod(DateFrom, DateTo)` [mismo helper, duplicar en este handler]
4. `GetConfirmedByPropertyAsync(PropertyId, from, to)` → reservations
5. `GetByPropertyIdAsync(PropertyId)` → allReservations (con navegaciones cargadas, para RecentReservations)
6. Calcular métricas (ver design.md)
7. `RecentReservations = allReservations.Take(5).Select(ToReservationDto).ToList()`
8. `Result.Success(dto)`

`ToReservationDto` privado y estático:
```csharp
private static ReservationDto ToReservationDto(Reservation r) =>
    new(r.Id, r.PropertyId, r.Property.Name, r.GuestId,
        $"{r.Guest.FirstName} {r.Guest.LastName}",
        r.CheckInDate, r.CheckOutDate, r.CheckInDateTime, r.CheckOutDateTime,
        r.TotalNights, r.TotalPrice, r.Status.ToString());
```

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-04 — Infrastructure: implementar nuevos métodos en ReservationRepository

**Archivo a modificar:**
- `src/Milo.Infraestructure/Persistence/Repositories/ReservationRepository.cs`

Agregar los dos métodos nuevos al final de la clase (antes del `SaveChangesAsync`):

```csharp
public async Task<IReadOnlyList<Reservation>> GetConfirmedByOwnerAsync(
    Guid ownerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    => await dbContext.Reservations
           .Where(r => r.Property.OwnerId == ownerId
                    && r.Status == ReservationStatus.Confirmed
                    && r.CheckInDate >= from
                    && r.CheckInDate <= to)
           .ToListAsync(cancellationToken);

public async Task<IReadOnlyList<Reservation>> GetConfirmedByPropertyAsync(
    Guid propertyId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    => await dbContext.Reservations
           .Where(r => r.PropertyId == propertyId
                    && r.Status == ReservationStatus.Confirmed
                    && r.CheckInDate >= from
                    && r.CheckInDate <= to)
           .ToListAsync(cancellationToken);
```

Sin Include — los handlers solo usan campos escalares de Reservation para métricas.
EF Core genera JOIN implícito en `GetConfirmedByOwnerAsync` para `r.Property.OwnerId`.

**Checkpoint:** `dotnet build --no-incremental --nologo` en la raíz sin errores.

---

## T-05 — API: DashboardController

**Archivo a crear:**
- `src/Milo.Api/Controllers/DashboardController.cs`

`[Authorize(Roles = "Owner")]` a nivel de clase.

| Método | Ruta | Respuesta |
|---|---|---|
| GET | /api/dashboard | 200 Ok + OwnerDashboardDto (siempre) |
| GET | /api/dashboard/properties/{propertyId:guid} | 200 Ok + PropertyMetricsDto ó 404 |

`GetDashboard`: `Ok(result.Value)` directo.
`GetPropertyMetrics`: `result.IsSuccess ? Ok(result.Value) : Problem(title: result.Error!, statusCode: 404)`.

`dateFrom` y `dateTo` como `[FromQuery] DateOnly?` — ASP.NET Core parsea `DateOnly` desde query string en formato `yyyy-MM-dd`.

**Checkpoint:** `dotnet build --no-incremental --nologo` en la raíz sin errores.

---

## Verificación final

- [ ] `dotnet build` raíz: 0 errores, 0 warnings
- [ ] Swagger muestra `/api/dashboard` y `/api/dashboard/properties/{propertyId}`
- [ ] `GET /api/dashboard` sin token → 401; con Guest → 403
- [ ] `GET /api/dashboard` con token Owner sin inmuebles → 200 con ceros
- [ ] `GET /api/dashboard` con Owner con inmuebles y reservas → métricas correctas
- [ ] `GET /api/dashboard?dateFrom=2026-01-01&dateTo=2026-06-30` → período específico
- [ ] `GET /api/dashboard/properties/{id}` con id de inmueble ajeno → 404
- [ ] `GET /api/dashboard/properties/{id}` propio → 200 con RecentReservations (max 5)
- [ ] `OccupancyRate` es 0 cuando no hay reservas (no dividir por cero)
- [ ] `ReservationsByProperty` ordenado por Revenue DESC
- [ ] Actualizar `progress/current.md`
