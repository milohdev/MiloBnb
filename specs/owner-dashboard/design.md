# Feature: owner-dashboard — Design

## Nuevos paquetes NuGet
Ninguno.

## Sin migration
Feature de solo lectura — no hay entidades nuevas.

---

## Estructura de carpetas nueva

```
src/
├── Milo.Domain/
│   └── Repositories/
│       └── IReservationRepository.cs                  ← 2 métodos nuevos
│
├── Milo.Application/
│   └── Dashboard/
│       ├── Queries/
│       │   ├── GetOwnerDashboard/
│       │   │   ├── OwnerDashboardDto.cs
│       │   │   ├── PropertySummaryDto.cs
│       │   │   ├── GetOwnerDashboardQuery.cs
│       │   │   └── GetOwnerDashboardHandler.cs
│       │   └── GetPropertyMetrics/
│       │       ├── PropertyMetricsDto.cs
│       │       ├── GetPropertyMetricsQuery.cs
│       │       └── GetPropertyMetricsHandler.cs
│
├── Milo.Infraestructure/
│   └── Persistence/
│       └── Repositories/
│           └── ReservationRepository.cs               ← 2 métodos nuevos
│
└── Milo.Api/
    └── Controllers/
        └── DashboardController.cs                     ← nueva
```

---

## Domain — IReservationRepository (modificación)

Agregar al final de la interfaz:

```csharp
Task<IReadOnlyList<Reservation>> GetConfirmedByOwnerAsync(
    Guid ownerId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

Task<IReadOnlyList<Reservation>> GetConfirmedByPropertyAsync(
    Guid propertyId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
```

`GetConfirmedByOwnerAsync`: filtra `Status == Confirmed`, `Property.OwnerId == ownerId`,
`CheckInDate >= from && CheckInDate <= to`. EF Core genera JOIN con Properties (via nav property).
No necesita Include de navegaciones — solo campos escalares de Reservation son necesarios para métricas.

`GetConfirmedByPropertyAsync`: filtra `PropertyId == propertyId`, `Status == Confirmed`,
`CheckInDate >= from && CheckInDate <= to`. También sin Include.

---

## Application — DTOs

### `OwnerDashboardDto.cs`

```csharp
public record OwnerDashboardDto(
    int TotalProperties,
    int TotalReservations,
    decimal TotalRevenue,
    double OccupancyRate,
    decimal AveragePricePerNight,
    DateOnly DateFrom,
    DateOnly DateTo,
    IReadOnlyList<PropertySummaryDto> ReservationsByProperty);
```

### `PropertySummaryDto.cs`

```csharp
public record PropertySummaryDto(
    Guid PropertyId,
    string PropertyName,
    int ReservationCount,
    decimal Revenue);
```

### `PropertyMetricsDto.cs`

```csharp
public record PropertyMetricsDto(
    Guid PropertyId,
    string PropertyName,
    decimal PricePerNight,
    bool AllowSameDayBooking,
    int TotalReservations,
    decimal TotalRevenue,
    double OccupancyRate,
    double AverageLengthOfStay,
    DateOnly DateFrom,
    DateOnly DateTo,
    IReadOnlyList<ReservationDto> RecentReservations);
```

`ReservationDto` importado de `Milo.Application.Reservations.Queries.GetMyReservations`.

---

## Application — Queries

### `GetOwnerDashboardQuery.cs`

```csharp
public record GetOwnerDashboardQuery(DateOnly? DateFrom, DateOnly? DateTo)
    : IRequest<Result<OwnerDashboardDto>>;
```

### `GetOwnerDashboardHandler.cs`

Inyecta: `IPropertyRepository, IReservationRepository, ICurrentUserProvider`

```
1. ownerId = currentUser.UserId!.Value
2. (from, to) = ResolvePeriod(DateFrom, DateTo)
3. allProperties = await propertyRepository.GetByOwnerIdAsync(ownerId, ct)
4. activeProperties = allProperties.Where(p => p.IsActive).ToList()
5. reservations = await reservationRepository.GetConfirmedByOwnerAsync(ownerId, from, to, ct)
6. Calcular métricas:
   - TotalProperties = activeProperties.Count
   - TotalReservations = reservations.Count
   - TotalRevenue = reservations.Sum(r => r.TotalPrice)
   - totalDays = to.DayNumber - from.DayNumber
   - occupiedDays = reservations.Sum(r => r.TotalNights)
   - OccupancyRate = totalDays > 0 && TotalProperties > 0
       ? (double)occupiedDays / ((double)totalDays * TotalProperties) * 100
       : 0d
   - AveragePricePerNight = activeProperties.Count > 0
       ? activeProperties.Average(p => p.PricePerNight)
       : 0m
   - ReservationsByProperty = activeProperties.Select(p => new PropertySummaryDto(
         p.Id, p.Name,
         reservations.Count(r => r.PropertyId == p.Id),
         reservations.Where(r => r.PropertyId == p.Id).Sum(r => r.TotalPrice)
     )).OrderByDescending(x => x.Revenue).ToList()
7. Result.Success(new OwnerDashboardDto(...))
```

**Helper privado estático en el handler:**

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

### `GetPropertyMetricsQuery.cs`

```csharp
public record GetPropertyMetricsQuery(Guid PropertyId, DateOnly? DateFrom, DateOnly? DateTo)
    : IRequest<Result<PropertyMetricsDto>>;
```

### `GetPropertyMetricsHandler.cs`

Inyecta: `IPropertyRepository, IReservationRepository, ICurrentUserProvider`

```
1. ownerId = currentUser.UserId!.Value
2. property = await propertyRepository.GetByIdAsync(PropertyId, ct)
   → if null || property.OwnerId != ownerId → Result.Failure("Inmueble no encontrado")  [404]
3. (from, to) = ResolvePeriod(DateFrom, DateTo)   [mismo helper]
4. reservations = await reservationRepository.GetConfirmedByPropertyAsync(PropertyId, from, to, ct)
5. allReservations = await reservationRepository.GetByPropertyIdAsync(PropertyId, ct)
   [ya cargado con Property+Guest, ordenado CheckInDate DESC — para RecentReservations]
6. Calcular:
   - TotalReservations = reservations.Count
   - TotalRevenue = reservations.Sum(r => r.TotalPrice)
   - totalDays = to.DayNumber - from.DayNumber
   - occupiedDays = reservations.Sum(r => r.TotalNights)
   - OccupancyRate = totalDays > 0
       ? (double)occupiedDays / (double)totalDays * 100
       : 0d
   - AverageLengthOfStay = reservations.Count > 0
       ? reservations.Average(r => (double)r.TotalNights)
       : 0d
   - RecentReservations = allReservations.Take(5).Select(ToReservationDto).ToList()
7. Result.Success(new PropertyMetricsDto(...))
```

`ToReservationDto` privado y estático — mismo mapping que Feature 3:
```csharp
private static ReservationDto ToReservationDto(Reservation r) =>
    new(r.Id, r.PropertyId, r.Property.Name, r.GuestId,
        $"{r.Guest.FirstName} {r.Guest.LastName}",
        r.CheckInDate, r.CheckOutDate, r.CheckInDateTime, r.CheckOutDateTime,
        r.TotalNights, r.TotalPrice, r.Status.ToString());
```

`ResolvePeriod` duplicado como private static en este handler también (mismo código).

---

## Infrastructure — ReservationRepository (modificación)

Agregar los dos métodos nuevos al final de la clase:

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

Sin Include: solo se acceden campos escalares de `Reservation` en los handlers de dashboard.
EF Core genera JOIN implícito para `r.Property.OwnerId` en la primera query.

---

## API — DashboardController

```csharp
[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Owner")]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
    {
        var result = await sender.Send(new GetOwnerDashboardQuery(dateFrom, dateTo), ct);
        return Ok(result.Value);
    }

    [HttpGet("properties/{propertyId:guid}")]
    public async Task<IActionResult> GetPropertyMetrics(
        Guid propertyId,
        [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo, CancellationToken ct)
    {
        var result = await sender.Send(new GetPropertyMetricsQuery(propertyId, dateFrom, dateTo), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
    }
}
```

`GetDashboard` siempre retorna 200 — incluso con listas vacías y métricas en cero.

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| `CheckInDate >= from && CheckInDate <= to` como filtro de período | La reserva "pertenece" al período por su fecha de inicio; evitar ambigüedad con reservas que cruzan el límite del período |
| `average(p => p.PricePerNight)` en LINQ in-memory | La lista de inmuebles ya está cargada; un AVG en BD sería una query extra innecesaria |
| `GetByPropertyIdAsync` para RecentReservations (no `GetConfirmedByPropertyAsync`) | Las últimas 5 reservas incluyen todos los estados (Confirmed, Cancelled) como esperaría un Owner |
| Sin Include en `GetConfirmedBy*` | Los handlers no necesitan navegar a `Property.Name` ni `Guest.*` para calcular métricas — solo escalares de Reservation |
| `ResolvePeriod` duplicado en ambos handlers | Evitar crear una clase compartida por solo 2 consumidores; la duplicación es mínima y los handlers son autónomos |
| Epoch `2020-01-01` cuando solo hay `dateTo` | La plataforma no tiene datos anteriores a su fecha de arranque; "desde inicio" se aproxima con una fecha razonable sin usar `DateOnly.MinValue` que rompería cálculos de ocupación |
| `AveragePricePerNight` es `decimal` | Es un valor monetario — mantener precisión exacta |
| `OccupancyRate` y `AverageLengthOfStay` son `double` | Son proporciones y promedios estadísticos — `double` es idiomático para estos cálculos |
