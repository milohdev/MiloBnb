# Feature: reservations — Design

## Nuevos paquetes NuGet
Ninguno. Todo el stack necesario ya está disponible.

---

## Estructura de carpetas nueva

```
src/
├── Milo.Domain/
│   ├── Constants/
│   │   └── BookingConstants.cs                          ← nueva
│   ├── Entities/
│   │   └── Reservation.cs                               ← nueva
│   ├── Entities/Enums/
│   │   └── ReservationStatus.cs                         ← nueva
│   └── Repositories/
│       └── IReservationRepository.cs                    ← nueva
│
├── Milo.Application/
│   └── Reservations/
│       ├── Commands/
│       │   ├── CreateReservation/
│       │   │   ├── CreateReservationCommand.cs
│       │   │   ├── CreateReservationCommandValidator.cs
│       │   │   └── CreateReservationHandler.cs
│       │   └── CancelReservation/
│       │       ├── CancelReservationCommand.cs
│       │       └── CancelReservationHandler.cs
│       └── Queries/
│           ├── GetMyReservations/
│           │   ├── GetMyReservationsQuery.cs
│           │   ├── GetMyReservationsHandler.cs
│           │   └── ReservationDto.cs                    ← compartido con GetPropertyReservations
│           └── GetPropertyReservations/
│               ├── GetPropertyReservationsQuery.cs
│               └── GetPropertyReservationsHandler.cs
│
├── Milo.Infraestructure/
│   └── Persistence/
│       ├── Configurations/
│       │   └── ReservationConfiguration.cs              ← nueva
│       └── Repositories/
│           └── ReservationRepository.cs                 ← nueva
│
└── Milo.Api/
    └── Controllers/
        └── ReservationsController.cs                    ← nueva
```

**Archivos modificados:**
- `Milo.Infraestructure/Persistence/MiloDbContext.cs` — agregar `DbSet<Reservation>`
- `Milo.Infraestructure/Persistence/Repositories/PropertyRepository.cs` — reemplazar TODO
- `Milo.Infraestructure/DependencyInjection.cs` — registrar `IReservationRepository`

---

## Domain — Constantes y Enum

### `BookingConstants.cs`

```csharp
namespace Milo.Domain.Constants;

public static class BookingConstants
{
    public const int CheckInHour = 14;   // 14:00 UTC
    public const int CheckOutHour = 12;  // 12:00 UTC (mediodía)
}
```

### `ReservationStatus.cs`

```csharp
namespace Milo.Domain.Entities.Enums;

public enum ReservationStatus
{
    Pending = 1,    // reservado para extensiones futuras
    Confirmed = 2,
    Cancelled = 3
}
```

---

## Domain — Entidad Reservation

### `Reservation.cs`

```csharp
namespace Milo.Domain.Entities;

public sealed class Reservation : BaseEntity, IAuditable, ISoftDeletable
{
    private Reservation() { }

    public Guid PropertyId { get; private set; }
    public Guid GuestId { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public DateTime CheckInDateTime { get; private set; }
    public DateTime CheckOutDateTime { get; private set; }
    public int TotalNights { get; private set; }
    public decimal TotalPrice { get; private set; }
    public ReservationStatus Status { get; private set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navegación EF — cargadas con .Include en el repo
    public Property Property { get; private set; } = null!;
    public User Guest { get; private set; } = null!;

    public static Reservation Create(
        Guid propertyId, Guid guestId,
        DateOnly checkInDate, DateOnly checkOutDate,
        decimal pricePerNight) =>
        new()
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            GuestId = guestId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            CheckInDateTime = checkInDate.ToDateTime(
                new TimeOnly(BookingConstants.CheckInHour, 0), DateTimeKind.Utc),
            CheckOutDateTime = checkOutDate.ToDateTime(
                new TimeOnly(BookingConstants.CheckOutHour, 0), DateTimeKind.Utc),
            TotalNights = checkOutDate.DayNumber - checkInDate.DayNumber,
            TotalPrice = (checkOutDate.DayNumber - checkInDate.DayNumber) * pricePerNight,
            Status = ReservationStatus.Confirmed
        };

    public void Cancel() => Status = ReservationStatus.Cancelled;
}
```

**Nota sobre ISoftDeletable**: el `IsDeleted` nunca se activa por cancelación — la cancelación
solo cambia `Status`. La implementación ISoftDeletable existe por consistencia de arquitectura
(el query filter global aplica pero no interfiere, ya que nunca se hace soft-delete en este flujo).

---

## Domain — Repositorio

### `IReservationRepository.cs`

```csharp
namespace Milo.Domain.Repositories;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetByGuestIdAsync(Guid guestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reservation>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingReservationAsync(
        Guid propertyId, DateOnly checkIn, DateOnly checkOut,
        Guid? excludeReservationId = null,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Crea la reserva dentro de una transacción Serializable para prevenir double-booking.
    /// Retorna false si ya existe una reserva solapada (sin lanzar excepción).
    /// </summary>
    Task<bool> TryCreateSerializableAsync(Reservation reservation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`TryCreateSerializableAsync` encapsula en Infrastructure toda la complejidad de la transacción
serializable, manteniendo Application libre de dependencias EF Core.

---

## Application — DTO

### `ReservationDto.cs` (en `Reservations/Queries/GetMyReservations/`)

```csharp
namespace Milo.Application.Reservations.Queries.GetMyReservations;

public record ReservationDto(
    Guid Id,
    Guid PropertyId,
    string PropertyName,
    Guid GuestId,
    string GuestFullName,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    DateTime CheckInDateTime,
    DateTime CheckOutDateTime,
    int TotalNights,
    decimal TotalPrice,
    string Status);
```

Mapeo auxiliar (método privado estático en cada handler que lo necesite):

```csharp
private static ReservationDto ToDto(Reservation r) =>
    new(r.Id, r.PropertyId, r.Property.Name, r.GuestId,
        $"{r.Guest.FirstName} {r.Guest.LastName}",
        r.CheckInDate, r.CheckOutDate, r.CheckInDateTime, r.CheckOutDateTime,
        r.TotalNights, r.TotalPrice, r.Status.ToString());
```

---

## Application — Commands

### `CreateReservationCommand.cs`

```csharp
public record CreateReservationCommand(
    Guid PropertyId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate) : IRequest<Result<ReservationDto>>;
```

### `CreateReservationCommandValidator.cs`

```csharp
public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.CheckInDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("La fecha de check-in no puede ser en el pasado");
        RuleFor(x => x.CheckOutDate)
            .GreaterThan(x => x.CheckInDate)
            .WithMessage("La fecha de check-out debe ser posterior a la fecha de check-in");
    }
}
```

**Nota**: El validador permite que `CheckInDate == hoy` — la restricción de AllowSameDayBooking
es una regla de negocio evaluada en el handler donde se conoce el valor de la Property.

### `CreateReservationHandler.cs`

```csharp
public sealed class CreateReservationHandler(
    IPropertyRepository propertyRepository,
    IUserRepository userRepository,
    IReservationRepository reservationRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<CreateReservationCommand, Result<ReservationDto>>
{
    public async Task<Result<ReservationDto>> Handle(
        CreateReservationCommand request, CancellationToken cancellationToken)
    {
        // 1. Verificar KYC
        var user = await userRepository.GetByIdAsync(currentUser.UserId!.Value, cancellationToken);
        if (user is null) return Result<ReservationDto>.Failure("Usuario no encontrado");
        if (!user.IsKycVerified)
            return Result<ReservationDto>.Failure(
                "Debes completar la verificación de identidad antes de reservar");

        // 2. Verificar que el inmueble existe y está activo
        var property = await propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null || !property.IsActive)
            return Result<ReservationDto>.Failure("Inmueble no disponible");

        // 3. Regla AllowSameDayBooking
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var minCheckIn = property.AllowSameDayBooking ? today : today.AddDays(1);
        if (request.CheckInDate < minCheckIn)
        {
            var msg = property.AllowSameDayBooking
                ? "La fecha de check-in no puede ser en el pasado"
                : "Este inmueble no permite reservas para el mismo día; la fecha mínima es mañana";
            return Result<ReservationDto>.Failure(msg);
        }

        // 4. Crear entidad y persistir con transacción serializable (previene double-booking)
        var reservation = Reservation.Create(
            request.PropertyId, currentUser.UserId!.Value,
            request.CheckInDate, request.CheckOutDate,
            property.PricePerNight);

        var created = await reservationRepository.TryCreateSerializableAsync(reservation, cancellationToken);
        if (!created)
            return Result<ReservationDto>.Failure("Las fechas seleccionadas no están disponibles");

        // 5. Recargar con navigations para el DTO (Property y Guest)
        var full = await reservationRepository.GetByIdAsync(reservation.Id, cancellationToken);
        return Result<ReservationDto>.Success(ToDto(full!));
    }

    private static ReservationDto ToDto(Reservation r) =>
        new(r.Id, r.PropertyId, r.Property.Name, r.GuestId,
            $"{r.Guest.FirstName} {r.Guest.LastName}",
            r.CheckInDate, r.CheckOutDate, r.CheckInDateTime, r.CheckOutDateTime,
            r.TotalNights, r.TotalPrice, r.Status.ToString());
}
```

**Paso 5 — por qué recargar**: `TryCreateSerializableAsync` hace `dbContext.SaveChangesAsync`
dentro de una transacción; la entidad en memoria no tiene las navigations `Property` ni `Guest`
cargadas. `GetByIdAsync` las carga con `.Include`.

### `CancelReservationCommand.cs`

```csharp
public record CancelReservationCommand(Guid ReservationId) : IRequest<Result<bool>>;
```

### `CancelReservationHandler.cs`

```csharp
// Inyecta: IReservationRepository, ICurrentUserProvider
// 1. GetByIdAsync → null → Result.Failure("Reserva no encontrada") [404]
// 2. reservation.GuestId != currentUser.UserId → Result.Failure("No tienes permiso...") [403]
// 3. reservation.Status == Cancelled → Result.Failure("La reserva ya está cancelada") [400]
// 4. reservation.Cancel()
// 5. reservationRepository.SaveChangesAsync()
// 6. Result.Success(true) → 204
```

---

## Application — Queries

### `GetMyReservationsQuery.cs`

```csharp
public record GetMyReservationsQuery : IRequest<Result<IReadOnlyList<ReservationDto>>>;
```

### `GetMyReservationsHandler.cs`

```csharp
// Inyecta: IReservationRepository, ICurrentUserProvider
// 1. reservationRepository.GetByGuestIdAsync(currentUser.UserId!.Value, ct)
// 2. Result.Success(reservations.Select(ToDto).ToList())
```

### `GetPropertyReservationsQuery.cs`

```csharp
public record GetPropertyReservationsQuery(Guid PropertyId)
    : IRequest<Result<IReadOnlyList<ReservationDto>>>;
```

### `GetPropertyReservationsHandler.cs`

```csharp
// Inyecta: IPropertyRepository, IReservationRepository, ICurrentUserProvider
// 1. propertyRepository.GetByIdAsync(PropertyId) → null → Result.Failure [404]
// 2. property.OwnerId != currentUser.UserId → Result.Failure("No tienes permiso...") [403]
// 3. reservationRepository.GetByPropertyIdAsync(PropertyId, ct)
// 4. Result.Success(reservations.Select(ToDto).ToList())
```

---

## Infrastructure — ReservationConfiguration

### `ReservationConfiguration.cs`

```csharp
public sealed class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TotalPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Índices para las queries más comunes
        builder.HasIndex(r => r.GuestId);
        builder.HasIndex(r => r.PropertyId);
        builder.HasIndex(r => new { r.PropertyId, r.CheckInDate, r.CheckOutDate });

        builder.HasOne(r => r.Property)
               .WithMany()
               .HasForeignKey(r => r.PropertyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Guest)
               .WithMany()
               .HasForeignKey(r => r.GuestId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

## Infrastructure — MiloDbContext (modificación)

Agregar:
```csharp
public DbSet<Reservation> Reservations => Set<Reservation>();
```

El query filter global `ApplySoftDeleteFilters` aplica automáticamente a `Reservation`
(implementa ISoftDeletable) vía reflexión ya existente.

---

## Infrastructure — ReservationRepository

### `ReservationRepository.cs`

```csharp
public sealed class ReservationRepository(MiloDbContext dbContext) : IReservationRepository
{
    public async Task<Reservation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Reservations
               .Include(r => r.Property)
               .Include(r => r.Guest)
               .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Reservation>> GetByGuestIdAsync(Guid guestId, CancellationToken ct = default)
        => await dbContext.Reservations
               .Include(r => r.Property)
               .Include(r => r.Guest)
               .Where(r => r.GuestId == guestId)
               .OrderByDescending(r => r.CheckInDate)
               .ToListAsync(ct);

    public async Task<IReadOnlyList<Reservation>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default)
        => await dbContext.Reservations
               .Include(r => r.Property)
               .Include(r => r.Guest)
               .Where(r => r.PropertyId == propertyId)
               .OrderByDescending(r => r.CheckInDate)
               .ToListAsync(ct);

    public async Task<bool> HasOverlappingReservationAsync(
        Guid propertyId, DateOnly checkIn, DateOnly checkOut,
        Guid? excludeReservationId = null, CancellationToken ct = default)
        => await dbContext.Reservations.AnyAsync(r =>
               r.PropertyId == propertyId &&
               r.Status != ReservationStatus.Cancelled &&
               (excludeReservationId == null || r.Id != excludeReservationId) &&
               r.CheckInDate < checkOut &&
               r.CheckOutDate > checkIn, ct);

    public async Task<bool> TryCreateSerializableAsync(Reservation reservation, CancellationToken ct = default)
    {
        await using var tx = await dbContext.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var hasOverlap = await dbContext.Reservations.AnyAsync(r =>
            r.PropertyId == reservation.PropertyId &&
            r.Status != ReservationStatus.Cancelled &&
            r.CheckInDate < reservation.CheckOutDate &&
            r.CheckOutDate > reservation.CheckInDate, ct);

        if (hasOverlap)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);
}
```

**Nota sobre `IgnoreQueryFilters` en `TryCreateSerializableAsync`**: el query filter global
filtra `IsDeleted = false`, pero como las reservas nunca se soft-deletan, el filtro no interfiere.
Para la consulta de solapamiento, Status != Cancelled es suficiente.

---

## Infrastructure — PropertyRepository (modificación del TODO Feature 3)

Reemplazar el comentario TODO en `GetAllAsync` por el filtro real:

```csharp
// Antes (TODO Feature 3):
// TODO Feature 3: excluir propiedades con reservas solapadas entre checkIn y checkOut

// Después:
if (checkIn.HasValue && checkOut.HasValue)
{
    query = query.Where(p => !dbContext.Reservations.Any(r =>
        r.PropertyId == p.Id &&
        r.Status != ReservationStatus.Cancelled &&
        r.CheckInDate < checkOut.Value &&
        r.CheckOutDate > checkIn.Value));
}
```

Requiere `using Milo.Domain.Entities.Enums;` en PropertyRepository.

---

## Infrastructure — DependencyInjection (modificación)

Agregar una línea:
```csharp
services.AddScoped<IReservationRepository, ReservationRepository>();
```

---

## Infrastructure — Migration

```bash
dotnet ef migrations add AddReservations \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Validar en el archivo generado:
- Tabla `Reservations` con columnas correctas
- `TotalPrice` con `numeric(18,2)`
- `Status` como `character varying(20)`
- FK a Properties con Restrict
- FK a Users (GuestId) con Restrict
- Índices en GuestId, PropertyId y compuesto (PropertyId, CheckInDate, CheckOutDate)

---

## API — ReservationsController

```csharp
[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController(ISender sender) : ControllerBase
{
    [Authorize(Roles = "Guest")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateReservationCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        if (!result.IsSuccess) return MapError(result.Error!);
        return CreatedAtAction(nameof(GetMy), null, result.Value);
    }

    [Authorize(Roles = "Guest")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new CancelReservationCommand(id), ct);
        return result.IsSuccess ? NoContent() : MapError(result.Error!);
    }

    [Authorize(Roles = "Guest")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyReservationsQuery(), ct);
        return Ok(result.Value);
    }

    [Authorize(Roles = "Owner")]
    [HttpGet("property/{propertyId:guid}")]
    public async Task<IActionResult> GetByProperty(Guid propertyId, CancellationToken ct)
    {
        var result = await sender.Send(new GetPropertyReservationsQuery(propertyId), ct);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    private IActionResult MapError(string error)
    {
        if (error.Contains("permiso", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("identidad", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status403Forbidden);
        if (error.Contains("no encontrad", StringComparison.OrdinalIgnoreCase) ||
            error.Contains("no disponible", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status404NotFound);
        if (error.Contains("fechas seleccionadas", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status409Conflict);
        return Problem(title: error, statusCode: StatusCodes.Status400BadRequest);
    }
}
```

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| `TryCreateSerializableAsync` en repo (no en handler) | Application no puede referenciar EF Core; la transacción serializable es un detalle de infraestructura |
| Recarga con `GetByIdAsync` tras el create | `TryCreateSerializableAsync` no deja las navigations cargadas; una segunda query es el costo correcto para devolver el DTO completo |
| `HasOverlappingReservationAsync` separado de `TryCreateSerializableAsync` | Reutilizable en futuras features (ej. editar reserva, verificar disponibilidad sin crear) |
| Filtro fechas en PropertyRepository usa `dbContext.Reservations` directamente | PropertyRepository ya tiene la instancia de DbContext; no necesita inyectar IReservationRepository (evita dependencia circular entre repos) |
| `Status` almacenado como `string` (no `int`) | Legibilidad en la base de datos, consistente con UserRole y la convención del proyecto |
| ISoftDeletable en Reservation | Consistencia arquitectural; `IsDeleted` nunca se activa en este flujo (cancelación ≠ soft-delete) |
| `GetMyReservationsHandler` no valida IsKycVerified | El Guest ya tiene reservas previas; KYC solo se valida al crear |
| `minCheckIn` calculado en el handler con `DateTime.UtcNow` | Los validators de FluentValidation también usan UTC; ambas capas deben ser consistentes |
