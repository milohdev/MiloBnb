# Feature: reservations — Tasks

> Implementar en este orden. `dotnet build --no-incremental --nologo` después de cada tarea.
> Firmas de referencia en `design.md`. Al terminar, ejecutar migration y verificar Swagger.

---

## T-01 — Domain: BookingConstants + ReservationStatus + Reservation

**Archivos a crear:**
- `src/Milo.Domain/Constants/BookingConstants.cs`
- `src/Milo.Domain/Entities/Enums/ReservationStatus.cs`
- `src/Milo.Domain/Entities/Reservation.cs`

`BookingConstants`: clase estática con `const int CheckInHour = 14` y `const int CheckOutHour = 12`.

`ReservationStatus`: enum con `Pending = 1`, `Confirmed = 2`, `Cancelled = 3`.

`Reservation` extiende `BaseEntity`, implementa `IAuditable` e `ISoftDeletable`.
Navigation properties: `public Property Property { get; private set; } = null!;`
y `public User Guest { get; private set; } = null!;` (no se asignan en el factory, EF las hidrata).

Factory `Create(Guid propertyId, Guid guestId, DateOnly checkInDate, DateOnly checkOutDate, decimal pricePerNight)`:
- `TotalNights = checkOutDate.DayNumber - checkInDate.DayNumber`
- `TotalPrice = TotalNights * pricePerNight`
- `CheckInDateTime = checkInDate.ToDateTime(new TimeOnly(BookingConstants.CheckInHour, 0), DateTimeKind.Utc)`
- `CheckOutDateTime = checkOutDate.ToDateTime(new TimeOnly(BookingConstants.CheckOutHour, 0), DateTimeKind.Utc)`
- `Status = ReservationStatus.Confirmed`

Método de instancia: `public void Cancel() => Status = ReservationStatus.Cancelled;`

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-02 — Domain: IReservationRepository

**Archivo a crear:**
- `src/Milo.Domain/Repositories/IReservationRepository.cs`

Métodos:
```
Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
Task<IReadOnlyList<Reservation>> GetByGuestIdAsync(Guid guestId, CancellationToken ct = default)
Task<IReadOnlyList<Reservation>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default)
Task<bool> HasOverlappingReservationAsync(Guid propertyId, DateOnly checkIn, DateOnly checkOut,
    Guid? excludeReservationId = null, CancellationToken ct = default)
Task<bool> TryCreateSerializableAsync(Reservation reservation, CancellationToken ct = default)
Task SaveChangesAsync(CancellationToken ct = default)
```

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-03 — Application: ReservationDto + CreateReservation (Command/Validator/Handler)

**Archivos a crear:**
- `src/Milo.Application/Reservations/Queries/GetMyReservations/ReservationDto.cs`
- `src/Milo.Application/Reservations/Commands/CreateReservation/CreateReservationCommand.cs`
- `src/Milo.Application/Reservations/Commands/CreateReservation/CreateReservationCommandValidator.cs`
- `src/Milo.Application/Reservations/Commands/CreateReservation/CreateReservationHandler.cs`

`ReservationDto`: record con los 13 campos del spec (ver design.md).

`CreateReservationCommand`: record con `Guid PropertyId`, `DateOnly CheckInDate`, `DateOnly CheckOutDate`.

Validator:
- `PropertyId.NotEmpty()`
- `CheckInDate >= DateOnly.FromDateTime(DateTime.UtcNow.Date)` con mensaje "La fecha de check-in no puede ser en el pasado"
- `CheckOutDate > CheckInDate` con mensaje "La fecha de check-out debe ser posterior a la fecha de check-in"

Handler inyecta `IPropertyRepository`, `IUserRepository`, `IReservationRepository`, `ICurrentUserProvider`.
Flujo exacto (ver design.md sección CreateReservationHandler):
1. Verificar KYC del usuario → 403 si false
2. Verificar property existe y `IsActive` → 404 si no
3. Regla AllowSameDayBooking con `minCheckIn` calculado en UTC
4. `Reservation.Create(...)` con `property.PricePerNight`
5. `TryCreateSerializableAsync` → si false, `Result.Failure("Las fechas seleccionadas no están disponibles")`
6. `GetByIdAsync(reservation.Id)` para recargar con navigations → `ToDto(full!)`

`ToDto(Reservation r)`: método privado estático en el handler.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-04 — Application: CancelReservation (Command/Handler)

**Archivos a crear:**
- `src/Milo.Application/Reservations/Commands/CancelReservation/CancelReservationCommand.cs`
- `src/Milo.Application/Reservations/Commands/CancelReservation/CancelReservationHandler.cs`

`CancelReservationCommand(Guid ReservationId)` → `Result<bool>`.

Handler inyecta `IReservationRepository`, `ICurrentUserProvider`. Flujo:
1. `GetByIdAsync` → null → `Result.Failure("Reserva no encontrada")` [404]
2. `reservation.GuestId != currentUser.UserId` → `Result.Failure("No tienes permiso para cancelar esta reserva")` [403]
3. `reservation.Status == ReservationStatus.Cancelled` → `Result.Failure("La reserva ya está cancelada")` [400]
4. `reservation.Cancel()`
5. `reservationRepository.SaveChangesAsync()`
6. `Result.Success(true)` → 204

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-05 — Application: GetMyReservations + GetPropertyReservations (Queries/Handlers)

**Archivos a crear:**
- `src/Milo.Application/Reservations/Queries/GetMyReservations/GetMyReservationsQuery.cs`
- `src/Milo.Application/Reservations/Queries/GetMyReservations/GetMyReservationsHandler.cs`
- `src/Milo.Application/Reservations/Queries/GetPropertyReservations/GetPropertyReservationsQuery.cs`
- `src/Milo.Application/Reservations/Queries/GetPropertyReservations/GetPropertyReservationsHandler.cs`

`GetMyReservationsQuery`: record sin parámetros → `Result<IReadOnlyList<ReservationDto>>`.
Handler inyecta `IReservationRepository`, `ICurrentUserProvider`.
`GetByGuestIdAsync(currentUser.UserId!.Value)` → `Result.Success(items.Select(ToDto).ToList())`.

`GetPropertyReservationsQuery(Guid PropertyId)` → `Result<IReadOnlyList<ReservationDto>>`.
Handler inyecta `IPropertyRepository`, `IReservationRepository`, `ICurrentUserProvider`.
1. `propertyRepository.GetByIdAsync` → null → Result.Failure [404]
2. `property.OwnerId != currentUser.UserId` → `Result.Failure("No tienes permiso para ver las reservas de este inmueble")` [403]
3. `reservationRepository.GetByPropertyIdAsync` → `Result.Success(...)`

Ambos handlers definen su propio `private static ReservationDto ToDto(Reservation r)`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-06 — Infrastructure: ReservationConfiguration + DbContext + ReservationRepository

**Archivos a crear:**
- `src/Milo.Infraestructure/Persistence/Configurations/ReservationConfiguration.cs`
- `src/Milo.Infraestructure/Persistence/Repositories/ReservationRepository.cs`

**Archivos a modificar:**
- `src/Milo.Infraestructure/Persistence/MiloDbContext.cs` — agregar `DbSet<Reservation>`

`ReservationConfiguration`:
- `TotalPrice`: `HasPrecision(18, 2)`
- `Status`: `HasConversion<string>().HasMaxLength(20)`
- Índice en `GuestId`
- Índice en `PropertyId`
- Índice compuesto en `(PropertyId, CheckInDate, CheckOutDate)`
- FK `HasOne(r => r.Property).WithMany().HasForeignKey(r => r.PropertyId).OnDelete(Restrict)`
- FK `HasOne(r => r.Guest).WithMany().HasForeignKey(r => r.GuestId).OnDelete(Restrict)`

`ReservationRepository.TryCreateSerializableAsync`:
```csharp
await using var tx = await dbContext.Database
    .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

var hasOverlap = await dbContext.Reservations.AnyAsync(r =>
    r.PropertyId == reservation.PropertyId &&
    r.Status != ReservationStatus.Cancelled &&
    r.CheckInDate < reservation.CheckOutDate &&
    r.CheckOutDate > reservation.CheckInDate, cancellationToken);

if (hasOverlap) { await tx.RollbackAsync(cancellationToken); return false; }

dbContext.Reservations.Add(reservation);
await dbContext.SaveChangesAsync(cancellationToken);
await tx.CommitAsync(cancellationToken);
return true;
```

Métodos de lectura (`GetByIdAsync`, `GetByGuestIdAsync`, `GetByPropertyIdAsync`) deben incluir
`.Include(r => r.Property).Include(r => r.Guest)`.

**Checkpoint:** `dotnet build src/Milo.Infraestructure/Milo.Infraestructure.csproj --no-incremental` sin errores.

---

## T-07 — Infrastructure: DI + Migration + Fix PropertyRepository

**Archivo a modificar:**
- `src/Milo.Infraestructure/DependencyInjection.cs` — agregar `AddScoped<IReservationRepository, ReservationRepository>()`
- `src/Milo.Infraestructure/Persistence/Repositories/PropertyRepository.cs` — reemplazar TODO Feature 3

**Fix en PropertyRepository.GetAllAsync** (reemplaza el comentario TODO):
```csharp
if (checkIn.HasValue && checkOut.HasValue)
{
    query = query.Where(p => !dbContext.Reservations.Any(r =>
        r.PropertyId == p.Id &&
        r.Status != ReservationStatus.Cancelled &&
        r.CheckInDate < checkOut.Value &&
        r.CheckOutDate > checkIn.Value));
}
```
Requiere `using Milo.Domain.Entities.Enums;` al principio del archivo.

**Migration:**
```bash
dotnet ef migrations add AddReservations \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Revisar migration generada:
- Tabla `Reservations` con `TotalPrice numeric(18,2)`, `Status varchar(20)`
- FKs a Properties y Users con `RestrictDelete`
- Índice en `GuestId`, índice en `PropertyId`, índice compuesto `(PropertyId, CheckInDate, CheckOutDate)`

**Checkpoint:** `dotnet build --no-incremental` en la raíz sin errores.

---

## T-08 — API: ReservationsController

**Archivo a crear:**
- `src/Milo.Api/Controllers/ReservationsController.cs`

Acciones y atributos:
| Método HTTP | Ruta | Rol |
|---|---|---|
| POST | /api/reservations | Guest |
| DELETE | /api/reservations/{id:guid} | Guest |
| GET | /api/reservations/my | Guest |
| GET | /api/reservations/property/{propertyId:guid} | Owner |

`Create` devuelve `CreatedAtAction(nameof(GetMy), null, result.Value)` (201).
`Cancel` devuelve `NoContent()` (204).
`GetMy` devuelve `Ok(result.Value)` (siempre éxito; ValidationBehavior maneja 400).
`GetByProperty` devuelve `Ok` o `MapError`.

`MapError`:
- "permiso" o "identidad" → 403
- "no encontrad" o "no disponible" → 404
- "fechas seleccionadas" → 409
- default → 400

**Checkpoint:** `dotnet build --no-incremental` en la raíz sin errores.

---

## Verificación final

Con PostgreSQL corriendo:

- [ ] `dotnet build` en raíz: 0 errores, 0 warnings
- [ ] Migration aplicada al arrancar: tabla `Reservations` creada con FKs e índices
- [ ] Swagger muestra los 4 endpoints de `/api/reservations`
- [ ] `POST /api/reservations` con token Guest (IsKycVerified=false) → 403
- [ ] `POST /api/reservations` con token Owner → 403 (rol incorrecto)
- [ ] `POST /api/reservations` con token Guest válido + propiedad existente + fechas válidas → 201 + ReservationDto
- [ ] Segunda `POST` con mismas fechas y mismo inmueble → 409 (double-booking)
- [ ] `POST` con CheckInDate en el pasado → 400 (FluentValidation)
- [ ] `POST` con CheckOutDate <= CheckInDate → 400 (FluentValidation)
- [ ] `DELETE /api/reservations/{id}` con el Guest dueño → 204
- [ ] `DELETE /api/reservations/{id}` de nuevo (ya cancelada) → 400
- [ ] `DELETE /api/reservations/{id}` con otro Guest → 403
- [ ] `GET /api/reservations/my` con token Guest → 200 + lista (incluye canceladas)
- [ ] `GET /api/reservations/property/{id}` con token Owner dueño → 200 + lista
- [ ] `GET /api/reservations/property/{id}` con Owner que no es dueño → 403
- [ ] `GET /api/properties?checkIn=2026-07-01&checkOut=2026-07-05` excluye inmuebles con reservas solapadas
- [ ] Actualizar `progress/current.md`
