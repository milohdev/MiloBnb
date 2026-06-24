# Feature: owner-dashboard — Requirements

## Contexto
Feature de solo lectura. No hay entidades nuevas ni migrations.
Todas las métricas se calculan en los handlers con LINQ sobre datos ya existentes.
Solo reservas con `Status = Confirmed` cuentan para revenue y ocupación.

---

## RF-01 — Dashboard general del Owner

`GET /api/dashboard?dateFrom=&dateTo=`  Solo rol `Owner`.

- Retorna métricas agregadas de todos los inmuebles activos (`IsActive = true`) del Owner.
- Parámetros opcionales: `dateFrom`, `dateTo` (ambos `DateOnly`).

### Resolución del período

| dateFrom | dateTo | Período resultante |
|---|---|---|
| null | null | últimos 30 días hasta hoy |
| presente | null | desde dateFrom hasta hoy |
| null | presente | desde 2020-01-01 hasta dateTo |
| presente | presente | rango exacto |

### Métricas retornadas (`OwnerDashboardDto`)

- `TotalProperties` — cantidad de inmuebles activos del Owner.
- `TotalReservations` — total de reservas Confirmed con `CheckInDate` dentro del período.
- `TotalRevenue` — suma de `TotalPrice` de esas reservas (`decimal`).
- `OccupancyRate` — `(Σ TotalNights / (días del período × TotalProperties)) × 100` (`double`, %).
  - Si `totalDays = 0` o `TotalProperties = 0` → `0`.
- `AveragePricePerNight` — promedio de `PricePerNight` de los inmuebles activos (`decimal`).
  - Si no hay inmuebles → `0`.
- `DateFrom`, `DateTo` — período efectivamente utilizado.
- `ReservationsByProperty` — lista de `PropertySummaryDto` ordenada por `Revenue DESC`.

### `PropertySummaryDto`
`PropertyId, PropertyName, ReservationCount, Revenue (decimal)`

Respuesta: **200 Ok** con `OwnerDashboardDto` (siempre, incluso si sin datos).

---

## RF-02 — Métricas de un inmueble individual

`GET /api/dashboard/properties/{propertyId}?dateFrom=&dateTo=`  Solo rol `Owner`.

- El inmueble debe pertenecer al Owner autenticado (`property.OwnerId == currentUser.UserId`).
  Si no existe o no pertenece: `Result.Failure` → **404 Not Found**.
- Misma lógica de resolución de período que RF-01.

### Métricas retornadas (`PropertyMetricsDto`)

- `PropertyId, PropertyName, PricePerNight, AllowSameDayBooking`
- `TotalReservations` — reservas Confirmed con `CheckInDate` en el período.
- `TotalRevenue` — suma de `TotalPrice`.
- `OccupancyRate` — `(Σ TotalNights / días del período) × 100` (`double`, %).
  - Si `totalDays = 0` → `0`.
- `AverageLengthOfStay` — promedio de `TotalNights` de las reservas (`double`).
  - Si no hay reservas → `0`.
- `DateFrom`, `DateTo` — período efectivo.
- `RecentReservations` — últimas 5 reservas del inmueble (cualquier estado), usando `ReservationDto` existente de Feature 3.

Respuesta: **200 Ok** con `PropertyMetricsDto` o **404**.

---

## Exclusiones
- Tests
- Gráficas / visualizaciones
- Comparativas entre períodos
- Exportación (es Feature 8)
- Métricas de Admin global
- Caché
