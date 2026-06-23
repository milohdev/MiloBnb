# Feature: reservations — Requirements

## Contexto
Gestión de reservas de inmuebles entre Guests y propiedades de Owners.
Esta feature introduce la entidad `Reservation`, prevención de double-booking con transacción
serializable, y cierra el TODO pendiente en el catálogo de propiedades (filtro por fechas).

---

## RF-01 — Crear reserva (Guest autenticado + KYC verificado)

`POST /api/reservations`

Solo usuarios con rol `Guest`. El GuestId se toma del JWT.

**Requisito previo de KYC**: Si `user.IsKycVerified == false`, retornar
`Result.Failure("Debes completar la verificación de identidad antes de reservar")` → 403.

Body: `PropertyId`, `CheckInDate` (DateOnly), `CheckOutDate` (DateOnly).

**Regla AllowSameDayBooking**:
- Leer `Property.AllowSameDayBooking`.
- Si `false`: `CheckInDate >= DateTime.UtcNow.Date.AddDays(1)` (mínimo mañana).
- Si `true`: `CheckInDate >= DateTime.UtcNow.Date` (puede ser hoy).
- Incumplimiento → `Result.Failure` con mensaje claro.

**Validación de fechas** (FluentValidation, 400 automático):
- `CheckInDate` no puede ser fecha pasada.
- `CheckOutDate > CheckInDate` (mínimo 1 noche).

**Prevención de double-booking** (transacción serializable):
- Verificar que no exista ninguna Reservation con `Status != Cancelled`
  que se solape con el rango solicitado para el mismo `PropertyId`.
- Solapamiento: `existingCheckIn < checkOut && existingCheckOut > checkIn`.
- La verificación y el INSERT ocurren en una sola transacción con
  `IsolationLevel.Serializable` para evitar race conditions.
- Si hay solapamiento → `Result.Failure("Las fechas seleccionadas no están disponibles")` → 409.

**Cálculo automático**:
- `TotalNights = CheckOutDate.DayNumber - CheckInDate.DayNumber`
- `TotalPrice = TotalNights * Property.PricePerNight`
- `CheckInDateTime = CheckInDate a las 14:00 UTC` (constante `BookingConstants.CheckInHour`)
- `CheckOutDateTime = CheckOutDate a las 12:00 UTC` (constante `BookingConstants.CheckOutHour`)
- `Status = Confirmed` (flujo directo, sin estado Pending intermedio)

Respuesta: `201 Created` con `ReservationDto`.

---

## RF-02 — Cancelar reserva (Guest dueño)

`DELETE /api/reservations/{id}`

Solo el Guest que creó la reserva (`reservation.GuestId == currentUser.UserId`).
Cambia `Status` a `Cancelled`. **No elimina físicamente** ni activa soft-delete.
Si el Guest no es el dueño → `Result.Failure` → 403.
Si ya está cancelada → `Result.Failure("La reserva ya está cancelada")` → 400.

Respuesta: `204 No Content`.

---

## RF-03 — Mis reservas (Guest autenticado)

`GET /api/reservations/my`

Retorna todas las reservas del Guest autenticado (incluye canceladas).
Ordenadas por `CheckInDate DESC`.

Respuesta: `200 Ok` con `IReadOnlyList<ReservationDto>`.

---

## RF-04 — Reservas de un inmueble (Owner dueño)

`GET /api/reservations/property/{propertyId}`

Solo el Owner dueño del inmueble (`property.OwnerId == currentUser.UserId`).
Retorna todas las reservas del inmueble (incluye canceladas).
Si el Owner no es el dueño → `Result.Failure` → 403.

Respuesta: `200 Ok` con `IReadOnlyList<ReservationDto>`.

---

## RF-05 — Filtro por fechas en catálogo de propiedades (cierre del TODO Feature 3)

El `TODO Feature 3` en `PropertyRepository.GetAllAsync` debe implementarse.

Cuando `checkIn` y `checkOut` vienen en `GET /api/properties`, excluir propiedades que tengan
reservas con `Status != Cancelled` solapadas con el rango solicitado.

El filtro se agrega como subconsulta EF Core directamente en `PropertyRepository` usando
`dbContext.Reservations` (la misma instancia de DbContext, sin depender de IReservationRepository).

---

## ReservationDto

```
Id, PropertyId, PropertyName, GuestId, GuestFullName,
CheckInDate, CheckOutDate, CheckInDateTime, CheckOutDateTime,
TotalNights, TotalPrice, Status
```

`PropertyName` y `GuestFullName` se cargan via navigation properties (`.Include`).
`GuestFullName = $"{user.FirstName} {user.LastName}"`.

---

## Exclusiones explícitas
- Pagos / cobros
- Penalizaciones por cancelación
- Notificaciones (Feature 6)
- Modificación de reservas (sin endpoint de edición)
- Reservas por Admin u Owner
- Tests unitarios o de integración
