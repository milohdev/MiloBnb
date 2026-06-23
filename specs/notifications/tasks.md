# Feature: notifications — Tasks

> Implementar en este orden. `dotnet build --no-incremental --nologo` después de cada tarea.
> Firmas de referencia en `design.md`. Al terminar, ejecutar migration y verificar Swagger.

---

## T-01 — Domain: NotificationType + Notification + PagedResult en Domain

**Archivos a crear:**
- `src/Milo.Domain/Entities/Enums/NotificationType.cs` — enum con 6 valores (ver design.md)
- `src/Milo.Domain/Entities/Notification.cs`
- `src/Milo.Domain/Common/PagedResult.cs`

`Notification` extiende `BaseEntity + IAuditable` (sin ISoftDeletable).
Propiedades: `UserId`, `Title`, `Body`, `Type (NotificationType)`, `IsRead (default false)`, `RelatedEntityId (Guid?)`.
Factory: `Create(Guid userId, string title, string body, NotificationType type, Guid? relatedEntityId = null)`.
Método: `MarkAsRead()` → `IsRead = true`.

`PagedResult<T>` en `Milo.Domain.Common` (namespace `Milo.Domain.Common`):
```csharp
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
```

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-02 — Domain: INotificationRepository

**Archivo a crear:**
- `src/Milo.Domain/Repositories/INotificationRepository.cs`

```csharp
// Imports: Milo.Domain.Common (PagedResult<T>), Milo.Domain.Entities (Notification)
Task<PagedResult<Notification>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
Task<IReadOnlyList<Notification>> GetUnreadByUserIdAsync(Guid userId, CancellationToken ct = default);
Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
void Add(Notification notification);
Task SaveChangesAsync(CancellationToken ct = default);
```

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-03 — Application: INotificationService

**Archivo a crear:**
- `src/Milo.Application/Common/Interfaces/INotificationService.cs`

```csharp
// Import: Milo.Domain.Entities.Enums
public interface INotificationService
{
    Task SendAsync(Guid userId, string title, string body,
        NotificationType type, Guid? relatedEntityId = null,
        CancellationToken cancellationToken = default);
}
```

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-04 — Application: NotificationDto + GetNotifications + GetUnread + MarkNotificationRead

**Archivos a crear:**
- `src/Milo.Application/Notifications/Queries/GetNotifications/NotificationDto.cs`
- `src/Milo.Application/Notifications/Queries/GetNotifications/GetNotificationsQuery.cs`
- `src/Milo.Application/Notifications/Queries/GetNotifications/GetNotificationsHandler.cs`
- `src/Milo.Application/Notifications/Queries/GetNotifications/GetUnreadNotificationsQuery.cs`
- `src/Milo.Application/Notifications/Queries/GetNotifications/GetUnreadNotificationsHandler.cs`
- `src/Milo.Application/Notifications/Commands/MarkNotificationRead/MarkNotificationReadCommand.cs`
- `src/Milo.Application/Notifications/Commands/MarkNotificationRead/MarkNotificationReadHandler.cs`

`NotificationDto`: `Id, Title, Body, Type (string), IsRead, RelatedEntityId (Guid?), CreatedAt`.

`GetNotificationsQuery(int Page = 1, int PageSize = 20)` → `Result<PagedResult<NotificationDto>>`.
Usar `PagedResult<T>` de `Milo.Domain.Common` (no el de Application).

`GetNotificationsHandler`:
1. `GetByUserIdAsync(userId, Page, PageSize, ct)` → retorna `PagedResult<Notification>` (Domain)
2. Mapear a `PagedResult<NotificationDto>`:
   ```csharp
   new PagedResult<NotificationDto>
   {
       Items = pagedNotifications.Items.Select(ToDto).ToList(),
       TotalCount = pagedNotifications.TotalCount,
       Page = pagedNotifications.Page,
       PageSize = pagedNotifications.PageSize
   }
   ```
3. `Result.Success(...)`.

`GetUnreadNotificationsQuery` → `Result<IReadOnlyList<NotificationDto>>`.
`GetUnreadNotificationsHandler`: `GetUnreadByUserIdAsync(userId, ct)` → map → `Result.Success`.

`MarkNotificationReadCommand(Guid NotificationId)` → `Result<bool>`.

`MarkNotificationReadHandler` inyecta `INotificationRepository, ICurrentUserProvider`:
1. `GetByIdAsync(NotificationId, ct)` → null → `Result.Failure("Notificación no encontrada")`
2. `notification.UserId != currentUser.UserId` → `Result.Failure("No tienes permiso para marcar esta notificación")`
3. `if (!notification.IsRead) { notification.MarkAsRead(); await notificationRepository.SaveChangesAsync(ct); }`
4. `Result.Success(true)`

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-05 — Application: Modificar handlers existentes

**Archivos a modificar:**
- `src/Milo.Application/Reservations/Commands/CreateReservation/CreateReservationHandler.cs`
- `src/Milo.Application/Reservations/Commands/CancelReservation/CancelReservationHandler.cs`
- `src/Milo.Application/Kyc/Commands/VerifyKyc/VerifyKycHandler.cs`

En cada uno: agregar `INotificationService notificationService` al constructor (primary constructor).

**CreateReservationHandler** — tras `var full = await reservationRepository.GetByIdAsync(...)`:
```csharp
var propertyName = full!.Property.Name;
var checkIn = full.CheckInDate.ToString("dd/MM/yyyy");
var checkOut = full.CheckOutDate.ToString("dd/MM/yyyy");

await notificationService.SendAsync(full.GuestId, "Reserva confirmada",
    $"Tu reserva en {propertyName} del {checkIn} al {checkOut} fue confirmada.",
    NotificationType.ReservationConfirmed, relatedEntityId: full.Id, cancellationToken);

await notificationService.SendAsync(full.Property.OwnerId, "Nueva reserva",
    $"Tienes una nueva reserva en {propertyName} del {checkIn} al {checkOut}.",
    NotificationType.ReservationConfirmed, relatedEntityId: full.Id, cancellationToken);
```

**CancelReservationHandler** — tras `await reservationRepository.SaveChangesAsync(cancellationToken)`:
(`reservation` ya tiene Property cargado via GetByIdAsync del repo)
```csharp
await notificationService.SendAsync(reservation.GuestId, "Reserva cancelada",
    $"Tu reserva en {reservation.Property.Name} del {reservation.CheckInDate:dd/MM/yyyy} al {reservation.CheckOutDate:dd/MM/yyyy} fue cancelada.",
    NotificationType.ReservationCancelled, relatedEntityId: reservation.Id, cancellationToken);
```

**VerifyKycHandler** — tras `await kycRepository.SaveChangesAsync(ct)`:
```csharp
if (verification.Status == KycStatus.Approved)
    await notificationService.SendAsync(userId, "Identidad verificada",
        "Tu identidad fue verificada exitosamente. Ya puedes realizar reservas.",
        NotificationType.KycApproved, relatedEntityId: verification.Id, cancellationToken);
else
    await notificationService.SendAsync(userId, "Verificación rechazada",
        $"No pudimos verificar tu identidad. Motivo: {verification.RejectionReason}",
        NotificationType.KycRejected, relatedEntityId: verification.Id, cancellationToken);
```

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-06 — Infrastructure: NotificationConfiguration + NotificationRepository + NotificationService + DI + DbContext

**Archivos a crear:**
- `src/Milo.Infraestructure/Persistence/Configurations/NotificationConfiguration.cs`
- `src/Milo.Infraestructure/Persistence/Repositories/NotificationRepository.cs`
- `src/Milo.Infraestructure/Services/NotificationService.cs`

**Archivos a modificar:**
- `src/Milo.Infraestructure/Persistence/MiloDbContext.cs` — `DbSet<Notification> Notifications`
- `src/Milo.Infraestructure/DependencyInjection.cs`:
  ```csharp
  services.AddScoped<INotificationService, NotificationService>();
  services.AddScoped<INotificationRepository, NotificationRepository>();
  ```

`NotificationConfiguration`:
- `HasKey(n => n.Id)`
- `HasIndex(n => n.UserId)`
- `HasIndex(n => new { n.UserId, n.IsRead })`
- `Type.HasConversion<string>().HasMaxLength(50)`
- `Title.HasMaxLength(200)`, `Body.HasMaxLength(1000)`
- `HasOne<User>().WithMany().HasForeignKey(n => n.UserId).OnDelete(Cascade)`

`NotificationRepository.GetByUserIdAsync`:
```csharp
var query = dbContext.Notifications
    .Where(n => n.UserId == userId)
    .OrderByDescending(n => n.CreatedAt);
var total = await query.CountAsync(ct);
var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
return new Milo.Domain.Common.PagedResult<Notification>
    { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
```

`NotificationService` inyecta `INotificationRepository, IUserRepository, ILogger<NotificationService>`.

**Checkpoint:** `dotnet build src/Milo.Infraestructure/Milo.Infraestructure.csproj --no-incremental` sin errores.

---

## T-07 — Infrastructure: Migration

```bash
dotnet ef migrations add AddNotifications \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Validar en el archivo generado:
- [ ] Tabla `Notifications` con todas las columnas (`IsRead` bool, `RelatedEntityId` uuid nullable)
- [ ] `Type` como `character varying(50)`
- [ ] Índice en `UserId`
- [ ] Índice compuesto en `(UserId, IsRead)`
- [ ] FK a `Users` con Cascade

**Checkpoint:** `dotnet build --no-incremental --nologo` en la raíz sin errores.

---

## T-08 — API: NotificationsController

**Archivo a crear:**
- `src/Milo.Api/Controllers/NotificationsController.cs`

`[Authorize]` a nivel de clase (sin rol — Guest y Owner pueden recibir notificaciones).

| Método | Ruta | Params | Respuesta |
|---|---|---|---|
| GET | /api/notifications | `?page&pageSize` | 200 Ok + PagedResult |
| GET | /api/notifications/unread | — | 200 Ok + lista |
| PUT | /api/notifications/{id:guid}/read | — | 204 / 403 / 404 |

`GetAll` y `GetUnread`: siempre retornan 200 (`result.Value` directo).
`MarkAsRead`: `result.IsSuccess ? NoContent() : MapError(result.Error)`.

`MapError`:
```csharp
private IActionResult MapError(string error) => error switch
{
    var e when e.Contains("permiso") =>
        Problem(title: e, statusCode: StatusCodes.Status403Forbidden),
    _ => Problem(title: error, statusCode: StatusCodes.Status404NotFound)
};
```

**Checkpoint:** `dotnet build --no-incremental --nologo` en la raíz sin errores.

---

## Verificación final

- [ ] `dotnet build` raíz: 0 errores, 0 warnings
- [ ] Migration aplicada: tabla `Notifications`, `Type varchar(50)`, índices correctos
- [ ] Swagger muestra 3 endpoints de `/api/notifications`
- [ ] `GET /api/notifications` sin token → 401
- [ ] Crear reserva → Guest y Owner reciben notificaciones en `GET /api/notifications`
- [ ] `GET /api/notifications/unread` → lista no vacía tras crear reserva
- [ ] `PUT /api/notifications/{id}/read` con id de otra persona → 403
- [ ] `PUT /api/notifications/{id}/read` con id propio → 204; repetir → 204 (idempotente)
- [ ] `GET /api/notifications/unread` → lista vacía tras marcar todas como leídas
- [ ] Cancelar reserva → notificación al Guest
- [ ] Verificar KYC → notificación `KycApproved` o `KycRejected`
- [ ] Log de Serilog muestra `"Email notification sent to {email} | Title: ... | Type: ..."`
- [ ] Actualizar `progress/current.md`
