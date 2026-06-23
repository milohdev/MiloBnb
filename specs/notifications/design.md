# Feature: notifications — Design

## Nuevos paquetes NuGet
Ninguno.

---

## Estructura de carpetas nueva

```
src/
├── Milo.Domain/
│   ├── Entities/
│   │   ├── Enums/
│   │   │   └── NotificationType.cs                              ← nueva
│   │   └── Notification.cs                                      ← nueva
│   └── Repositories/
│       └── INotificationRepository.cs                           ← nueva
│
├── Milo.Application/
│   ├── Common/
│   │   └── Interfaces/
│   │       └── INotificationService.cs                          ← nueva
│   └── Notifications/
│       ├── Commands/
│       │   └── MarkNotificationRead/
│       │       ├── MarkNotificationReadCommand.cs
│       │       └── MarkNotificationReadHandler.cs
│       └── Queries/
│           └── GetNotifications/
│               ├── GetNotificationsQuery.cs
│               ├── GetNotificationsHandler.cs
│               ├── GetUnreadNotificationsQuery.cs
│               ├── GetUnreadNotificationsHandler.cs
│               └── NotificationDto.cs
│
├── Milo.Infraestructure/
│   ├── Persistence/
│   │   └── Configurations/
│   │       └── NotificationConfiguration.cs                     ← nueva
│   ├── Persistence/
│   │   └── Repositories/
│   │       └── NotificationRepository.cs                        ← nueva
│   └── Services/
│       └── NotificationService.cs                               ← nueva
│
└── Milo.Api/
    └── Controllers/
        └── NotificationsController.cs                           ← nueva
```

**Archivos modificados:**
- `Milo.Application/Reservations/Commands/CreateReservation/CreateReservationHandler.cs`
- `Milo.Application/Reservations/Commands/CancelReservation/CancelReservationHandler.cs`
- `Milo.Application/Kyc/Commands/VerifyKyc/VerifyKycHandler.cs`
- `Milo.Infraestructure/Persistence/MiloDbContext.cs`
- `Milo.Infraestructure/DependencyInjection.cs`

---

## Domain — Enum NotificationType

### `NotificationType.cs`

```csharp
namespace Milo.Domain.Entities.Enums;

public enum NotificationType
{
    ReservationConfirmed = 1,
    ReservationCancelled = 2,
    KycApproved = 3,
    KycRejected = 4,
    CheckInReminder = 5,
    CheckOutReminder = 6
}
```

---

## Domain — Entidad Notification

### `Notification.cs`

```csharp
namespace Milo.Domain.Entities;

public sealed class Notification : BaseEntity, IAuditable
{
    private Notification() { }

    public Guid UserId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public Guid? RelatedEntityId { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static Notification Create(
        Guid userId, string title, string body,
        NotificationType type, Guid? relatedEntityId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Body = body,
            Type = type,
            IsRead = false,
            RelatedEntityId = relatedEntityId
        };

    public void MarkAsRead() => IsRead = true;
}
```

`Notification` **no implementa ISoftDeletable** — las notificaciones no se eliminan.

---

## Domain — Repositorio

### `INotificationRepository.cs`

```csharp
namespace Milo.Domain.Repositories;

public interface INotificationRepository
{
    Task<PagedResult<Notification>> GetByUserIdAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Notification notification);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`INotificationRepository` referencia `PagedResult<Notification>` — el tipo vive en Application.
Para respetar Clean Architecture (Domain no depende de Application), `PagedResult<T>` debe moverse
a Domain, o usar un tipo propio en Domain, o retornar una tupla.

**Decisión:** `PagedResult<T>` ya existe en `Milo.Application.Common.Models`. Para no violar
la regla Domain ← Application, se define en Domain o se usa un tipo alternativo en la interfaz.
**Solución:** definir `PagedResult<T>` también en `Milo.Domain.Common` (junto a `BaseEntity`).
La Application puede importarla desde Domain. El tipo de Application se puede mantener o eliminar.

> Ver T-01 — crear `PagedResult<T>` en `Milo.Domain.Common.PagedResult.cs` con las mismas
> propiedades que el de Application (`Items, TotalCount, Page, PageSize, TotalPages`).
> Actualizar los handlers existentes que retornan `PagedResult<T>` para usar el de Domain.
> El de Application se deja como alias (using = Milo.Domain.Common.PagedResult<T>) o se elimina.

---

## Application — INotificationService

### `INotificationService.cs`

```csharp
namespace Milo.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendAsync(
        Guid userId, string title, string body,
        NotificationType type, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);
}
```

`NotificationType` vive en `Milo.Domain.Entities.Enums` (accesible desde Application).

---

## Application — DTOs y Queries/Commands

### `NotificationDto.cs`

```csharp
public record NotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Type,
    bool IsRead,
    Guid? RelatedEntityId,
    DateTime CreatedAt);
```

### `GetNotificationsQuery.cs`

```csharp
public record GetNotificationsQuery(int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<NotificationDto>>>;
```

Handler inyecta `INotificationRepository, ICurrentUserProvider`:
```
1. notificationRepository.GetByUserIdAsync(userId, Page, PageSize, ct)
2. Result.Success(new PagedResult<NotificationDto> { Items = items.Items.Select(ToDto), ... })
```

### `GetUnreadNotificationsQuery.cs`

```csharp
public record GetUnreadNotificationsQuery : IRequest<Result<IReadOnlyList<NotificationDto>>>;
```

Handler: `GetUnreadByUserIdAsync(userId, ct)` → map → `Result.Success`.

### `MarkNotificationReadCommand.cs`

```csharp
public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result<bool>>;
```

Handler inyecta `INotificationRepository, ICurrentUserProvider`:
```
1. notificationRepository.GetByIdAsync(NotificationId, ct)
   → null → Result.Failure("Notificación no encontrada")           [404]
2. notification.UserId != currentUser.UserId
   → Result.Failure("No tienes permiso para marcar esta notificación")  [403]
3. if (!notification.IsRead) notification.MarkAsRead()              [idempotente]
4. await notificationRepository.SaveChangesAsync(ct)
5. Result.Success(true)
```

---

## Application — Modificaciones a handlers existentes

### `CreateReservationHandler.cs`

Agregar `INotificationService` al constructor.
Tras `var full = await reservationRepository.GetByIdAsync(...)`:

```csharp
var propertyName = full!.Property.Name;
var checkIn = full.CheckInDate.ToString("dd/MM/yyyy");
var checkOut = full.CheckOutDate.ToString("dd/MM/yyyy");

await notificationService.SendAsync(
    full.GuestId,
    "Reserva confirmada",
    $"Tu reserva en {propertyName} del {checkIn} al {checkOut} fue confirmada.",
    NotificationType.ReservationConfirmed,
    relatedEntityId: full.Id,
    cancellationToken);

await notificationService.SendAsync(
    full.Property.OwnerId,
    "Nueva reserva",
    $"Tienes una nueva reserva en {propertyName} del {checkIn} al {checkOut}.",
    NotificationType.ReservationConfirmed,
    relatedEntityId: full.Id,
    cancellationToken);
```

### `CancelReservationHandler.cs`

Agregar `INotificationService` al constructor.
La reserva cargada con `GetByIdAsync` ya incluye `Property` (navigation cargada en el repo).
Tras `await reservationRepository.SaveChangesAsync(cancellationToken)`:

```csharp
await notificationService.SendAsync(
    reservation.GuestId,
    "Reserva cancelada",
    $"Tu reserva en {reservation.Property.Name} del {reservation.CheckInDate:dd/MM/yyyy} al {reservation.CheckOutDate:dd/MM/yyyy} fue cancelada.",
    NotificationType.ReservationCancelled,
    relatedEntityId: reservation.Id,
    cancellationToken);
```

### `VerifyKycHandler.cs`

Agregar `INotificationService` al constructor.
Tras `await kycRepository.SaveChangesAsync(ct)`:

```csharp
if (verification.Status == KycStatus.Approved)
{
    await notificationService.SendAsync(
        userId,
        "Identidad verificada",
        "Tu identidad fue verificada exitosamente. Ya puedes realizar reservas.",
        NotificationType.KycApproved,
        relatedEntityId: verification.Id,
        cancellationToken);
}
else
{
    await notificationService.SendAsync(
        userId,
        "Verificación rechazada",
        $"No pudimos verificar tu identidad. Motivo: {verification.RejectionReason}",
        NotificationType.KycRejected,
        relatedEntityId: verification.Id,
        cancellationToken);
}
```

---

## Infrastructure — NotificationConfiguration

### `NotificationConfiguration.cs`

```csharp
builder.HasKey(n => n.Id);

builder.HasIndex(n => n.UserId);
builder.HasIndex(n => new { n.UserId, n.IsRead });

builder.Property(n => n.Type)
       .HasConversion<string>()
       .HasMaxLength(50);

builder.Property(n => n.Title).HasMaxLength(200);
builder.Property(n => n.Body).HasMaxLength(1000);

builder.HasOne<User>()
       .WithMany()
       .HasForeignKey(n => n.UserId)
       .OnDelete(DeleteBehavior.Cascade);
```

Cascade: si el usuario es eliminado físicamente, sus notificaciones se eliminan.
(En la práctica Users son soft-deleted, por lo que no ocurre.)

---

## Infrastructure — NotificationRepository

### `NotificationRepository.cs`

```csharp
public async Task<PagedResult<Notification>> GetByUserIdAsync(
    Guid userId, int page, int pageSize, CancellationToken ct = default)
{
    var query = dbContext.Notifications
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.CreatedAt);

    var total = await query.CountAsync(ct);
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

    return new PagedResult<Notification>
    {
        Items = items,
        TotalCount = total,
        Page = page,
        PageSize = pageSize
    };
}

public async Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(
    Guid userId, CancellationToken ct = default)
    => await dbContext.Notifications
           .Where(n => n.UserId == userId && !n.IsRead)
           .OrderByDescending(n => n.CreatedAt)
           .ToListAsync(ct);

public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
    => await dbContext.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
```

---

## Infrastructure — NotificationService

### `NotificationService.cs`

```csharp
public sealed class NotificationService(
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task SendAsync(
        Guid userId, string title, string body,
        NotificationType type, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(userId, title, body, type, relatedEntityId);
        notificationRepository.Add(notification);
        await notificationRepository.SaveChangesAsync(cancellationToken);

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        logger.LogInformation(
            "Email notification sent to {Email} | Title: {Title} | Type: {Type}",
            user?.Email ?? userId.ToString(), title, type);
    }
}
```

`NotificationService` es scoped — inyecta `INotificationRepository` (scoped) e `IUserRepository` (scoped).

---

## Infrastructure — DependencyInjection (modificación)

```csharp
services.AddScoped<INotificationService, NotificationService>();
services.AddScoped<INotificationRepository, NotificationRepository>();
```

---

## Infrastructure — MiloDbContext (modificación)

```csharp
public DbSet<Notification> Notifications => Set<Notification>();
```

`Notification` no implementa ISoftDeletable → el filtro global lo omite.

---

## Infrastructure — Migration

```bash
dotnet ef migrations add AddNotifications \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Validar:
- Tabla `Notifications`: `Id, UserId, Title, Body, Type varchar(50), IsRead, RelatedEntityId (uuid nullable), CreatedAt, UpdatedAt, CreatedBy, UpdatedBy`
- Índice en `UserId`
- Índice compuesto en `(UserId, IsRead)`
- FK a `Users` con Cascade

---

## API — NotificationsController

```csharp
[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetNotificationsQuery(page, pageSize), ct);
        return Ok(result.Value);
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread(CancellationToken ct)
    {
        var result = await sender.Send(new GetUnreadNotificationsQuery(), ct);
        return Ok(result.Value);
    }

    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new MarkNotificationReadCommand(id), ct);
        return result.IsSuccess
            ? NoContent()
            : MapError(result.Error);
    }

    private IActionResult MapError(string error) => error switch
    {
        var e when e.Contains("permiso") =>
            Problem(title: e, statusCode: StatusCodes.Status403Forbidden),
        _ => Problem(title: error, statusCode: StatusCodes.Status404NotFound)
    };
}
```

`[Authorize]` sin rol: cualquier usuario autenticado (Guest u Owner).
`GetAll` y `GetUnread` siempre retornan 200 (la lista puede estar vacía).

---

## Decisión clave: PagedResult en Domain

`INotificationRepository.GetByUserIdAsync` retorna `PagedResult<Notification>`.
`PagedResult<T>` vive actualmente en `Milo.Application.Common.Models` — Domain no puede referenciarlo.

**Solución:** crear `PagedResult<T>` en `Milo.Domain.Common` con las mismas propiedades.
Los handlers de Notifications (en Application) mapean de `PagedResult<Notification>` (Domain) a
`PagedResult<NotificationDto>` (ahora también puede ser el de Domain, ya que es el mismo tipo).

`GetPropertiesHandler`, `GetMyReservationsHandler`, etc. que usan el `PagedResult<T>` de Application:
se mantienen sin cambios — siguen usando el de Application. Son tipos distintos pero equivalentes.
No hay colisión porque los handlers de Notifications importan el de Domain explícitamente.

> Alternativa más limpia: mover `PagedResult<T>` completamente a Domain y eliminar el de Application.
> Pero esto requiere actualizar todos los handlers existentes. **Para minimizar cambios, se crea en Domain
> y se usa solo en el nuevo repositorio y handlers de Notifications.** Los handlers existentes siguen
> con el de Application sin modificación.

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| No ISoftDeletable en Notification | Las notificaciones son registros de auditoría; el spec no pide eliminación |
| `MarkAsRead()` idempotente en el handler | Si ya está leída, no llama `SaveChangesAsync` innecesariamente |
| `NotificationService` guarda en BD antes de loguear el email | El registro en BD es el dato crítico; el log es secundario. Si el log falla, la notificación ya está guardada |
| `PagedResult<T>` en Domain solo para Notifications | Minimiza cambios a handlers existentes, mantiene Clean Architecture |
| `[Authorize]` sin rol en NotificationsController | Tanto Guest como Owner reciben notificaciones |
| Índice compuesto `(UserId, IsRead)` | Optimiza la query de no leídas (`WHERE UserId = X AND IsRead = false`) |
| FK Cascade a Users | Si el usuario se eliminara físicamente, sus notificaciones se eliminarían automáticamente |
| Notificaciones síncronas en handlers | Spec explícito: "sincrónicamente dentro del handler". Si la notificación falla, falla el request — no hay enmascaramiento de errores |
