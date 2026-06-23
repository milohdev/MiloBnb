# Feature: wishlist — Design

## Nuevos paquetes NuGet
Ninguno.

---

## Estructura de carpetas nueva

```
src/
├── Milo.Domain/
│   ├── Entities/
│   │   └── WishlistItem.cs                              ← nueva
│   └── Repositories/
│       └── IWishlistRepository.cs                       ← nueva
│
├── Milo.Application/
│   └── Wishlist/
│       ├── Commands/
│       │   ├── AddToWishlist/
│       │   │   ├── AddToWishlistCommand.cs
│       │   │   └── AddToWishlistHandler.cs
│       │   └── RemoveFromWishlist/
│       │       ├── RemoveFromWishlistCommand.cs
│       │       └── RemoveFromWishlistHandler.cs
│       └── Queries/
│           └── GetWishlist/
│               ├── GetWishlistQuery.cs
│               └── GetWishlistHandler.cs
│
├── Milo.Infraestructure/
│   └── Persistence/
│       ├── Configurations/
│       │   └── WishlistItemConfiguration.cs             ← nueva
│       └── Repositories/
│           └── WishlistRepository.cs                    ← nueva
│
└── Milo.Api/
    └── Controllers/
        └── WishlistController.cs                        ← nueva
```

**Archivos modificados:**
- `Milo.Infraestructure/Persistence/MiloDbContext.cs` — agregar `DbSet<WishlistItem>`
- `Milo.Infraestructure/DependencyInjection.cs` — registrar `IWishlistRepository`

---

## Domain — Entidad WishlistItem

### `WishlistItem.cs`

```csharp
namespace Milo.Domain.Entities;

public sealed class WishlistItem : BaseEntity, IAuditable
{
    private WishlistItem() { }

    public Guid GuestId { get; private set; }
    public Guid PropertyId { get; private set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // Navegación EF — cargada con .Include en el repo
    public Property Property { get; private set; } = null!;

    public static WishlistItem Create(Guid guestId, Guid propertyId) =>
        new() { Id = Guid.NewGuid(), GuestId = guestId, PropertyId = propertyId };
}
```

`WishlistItem` **no implementa ISoftDeletable** — el borrado es físico.
El query filter global de EF no aplica sobre esta entidad.

Navegación `Property` permite cargar los datos del inmueble (nombre, imágenes, etc.)
para mapear directamente a `PropertyDto` sin queries adicionales.

---

## Domain — Repositorio

### `IWishlistRepository.cs`

```csharp
namespace Milo.Domain.Repositories;

public interface IWishlistRepository
{
    Task<IReadOnlyList<WishlistItem>> GetByGuestIdAsync(Guid guestId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid guestId, Guid propertyId, CancellationToken cancellationToken = default);
    void Add(WishlistItem item);
    Task DeleteAsync(Guid guestId, Guid propertyId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**Nota sobre `Add` vs `AddAsync`**: consistente con el resto del proyecto, `Add` es síncrono
(EF sólo encola el cambio) y `SaveChangesAsync` persiste. `DeleteAsync` es async porque necesita
hacer una query previa para encontrar la entidad antes de eliminarla.

`GetByGuestIdAsync` usa `.Include(w => w.Property).ThenInclude(p => p.Images)` para cargar
los datos necesarios para el DTO. Filtra por `w.Property.IsActive` (el query filter global
excluye `IsDeleted` automáticamente vía Include).

`DeleteAsync` es **idempotente**: si el ítem no existe, no hace nada (no lanza excepción).
Busca por `(GuestId, PropertyId)`, si lo encuentra elimina y guarda, si no simplemente retorna.

---

## Application — Commands

### `AddToWishlistCommand.cs`

```csharp
public record AddToWishlistCommand(Guid PropertyId) : IRequest<Result<bool>>;
```

### `AddToWishlistHandler.cs`

```csharp
// Inyecta: IPropertyRepository, IWishlistRepository, ICurrentUserProvider
// 1. propertyRepository.GetByIdAsync(PropertyId)
//    → null || !property.IsActive → Result.Failure("Inmueble no disponible") [404]
// 2. wishlistRepository.ExistsAsync(currentUser.UserId, PropertyId)
//    → true → Result.Success(true) [idempotente, 200]
// 3. WishlistItem.Create(guestId, PropertyId)
// 4. wishlistRepository.Add(item)
// 5. wishlistRepository.SaveChangesAsync()
// 6. Result.Success(true)
```

### `RemoveFromWishlistCommand.cs`

```csharp
public record RemoveFromWishlistCommand(Guid PropertyId) : IRequest<Result<bool>>;
```

### `RemoveFromWishlistHandler.cs`

```csharp
// Inyecta: IWishlistRepository, ICurrentUserProvider
// 1. wishlistRepository.DeleteAsync(currentUser.UserId!.Value, PropertyId, ct)
//    (idempotente — no error si no existe)
// 2. Result.Success(true)
```

---

## Application — Query

### `GetWishlistQuery.cs`

```csharp
public record GetWishlistQuery : IRequest<Result<IReadOnlyList<PropertyDto>>>;
```

Usa `PropertyDto` de `Milo.Application.Properties.Queries.GetProperties`.

### `GetWishlistHandler.cs`

```csharp
// Inyecta: IWishlistRepository, ICurrentUserProvider
// 1. wishlistRepository.GetByGuestIdAsync(currentUser.UserId!.Value, ct)
//    (ya filtra IsActive, IsDeleted excluido por query filter vía Include)
// 2. items.Select(ToDto).ToList() → Result.Success(dtos)
```

Mapeo auxiliar (`ToDto` privado y estático en el handler):

```csharp
private static PropertyDto ToDto(WishlistItem w) =>
    new(w.Property.Id, w.Property.Name, w.Property.Description,
        w.Property.Address, w.Property.City, w.Property.Country,
        w.Property.PricePerNight, w.Property.MaxGuests,
        w.Property.Bedrooms, w.Property.Bathrooms,
        w.Property.AllowSameDayBooking, w.Property.IsActive, w.Property.OwnerId,
        w.Property.Images.Select(i => i.Url).ToList(),
        w.Property.CreatedAt);
```

---

## Infrastructure — WishlistItemConfiguration

### `WishlistItemConfiguration.cs`

```csharp
public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.HasKey(w => w.Id);

        // Constraint único — evita duplicados a nivel BD
        builder.HasIndex(w => new { w.GuestId, w.PropertyId }).IsUnique();

        // Índice para GetByGuestIdAsync (query más frecuente)
        builder.HasIndex(w => w.GuestId);

        builder.HasOne(w => w.Property)
               .WithMany()
               .HasForeignKey(w => w.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(w => w.GuestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Cascade en ambas FKs: si un inmueble o usuario es eliminado físicamente,
los ítems de wishlist se eliminan automáticamente.
(En la práctica no ocurre ya que Properties y Users usan soft-delete.)

---

## Infrastructure — MiloDbContext (modificación)

Agregar:
```csharp
public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
```

`WishlistItem` no implementa ISoftDeletable → el método `ApplySoftDeleteFilters` lo omite
automáticamente (el `IsAssignableFrom` devuelve false). Sin filtro global.

---

## Infrastructure — WishlistRepository

### `WishlistRepository.cs`

```csharp
public sealed class WishlistRepository(MiloDbContext dbContext) : IWishlistRepository
{
    public async Task<IReadOnlyList<WishlistItem>> GetByGuestIdAsync(
        Guid guestId, CancellationToken cancellationToken = default)
        => await dbContext.WishlistItems
               .Include(w => w.Property)
                   .ThenInclude(p => p.Images)
               .Where(w => w.GuestId == guestId && w.Property.IsActive)
               .OrderByDescending(w => w.CreatedAt)
               .ToListAsync(cancellationToken);

    public async Task<bool> ExistsAsync(
        Guid guestId, Guid propertyId, CancellationToken cancellationToken = default)
        => await dbContext.WishlistItems.AnyAsync(
               w => w.GuestId == guestId && w.PropertyId == propertyId, cancellationToken);

    public void Add(WishlistItem item)
        => dbContext.WishlistItems.Add(item);

    public async Task DeleteAsync(
        Guid guestId, Guid propertyId, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.WishlistItems
            .FirstOrDefaultAsync(w => w.GuestId == guestId && w.PropertyId == propertyId,
                cancellationToken);

        if (item is not null)
        {
            dbContext.WishlistItems.Remove(item);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
```

---

## Infrastructure — DependencyInjection (modificación)

```csharp
services.AddScoped<IWishlistRepository, WishlistRepository>();
```

---

## Infrastructure — Migration

```bash
dotnet ef migrations add AddWishlist \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Validar en el archivo generado:
- Tabla `WishlistItems` con columnas `Id`, `GuestId`, `PropertyId`, `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`
- Índice UNIQUE en `(GuestId, PropertyId)`
- Índice en `GuestId`
- FK a `Properties` con Cascade
- FK a `Users` (GuestId) con Cascade

---

## API — WishlistController

```csharp
[ApiController]
[Route("api/wishlist")]
[Authorize(Roles = "Guest")]
public sealed class WishlistController(ISender sender) : ControllerBase
{
    [HttpPost("{propertyId:guid}")]
    public async Task<IActionResult> Add(Guid propertyId, CancellationToken ct)
    {
        var result = await sender.Send(new AddToWishlistCommand(propertyId), ct);
        return result.IsSuccess
            ? Ok()
            : Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    [HttpDelete("{propertyId:guid}")]
    public async Task<IActionResult> Remove(Guid propertyId, CancellationToken ct)
    {
        await sender.Send(new RemoveFromWishlistCommand(propertyId), ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetMy(CancellationToken ct)
    {
        var result = await sender.Send(new GetWishlistQuery(), ct);
        return Ok(result.Value);
    }
}
```

`[Authorize(Roles = "Guest")]` a nivel de clase aplica a las tres acciones.
`Remove` no verifica el `Result` porque la operación es siempre exitosa (idempotente).
`GetMy` siempre devuelve 200 (la lista puede estar vacía, no es un error).

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| No ISoftDeletable en WishlistItem | La naturaleza del favorito es ser eliminable físicamente — no hay necesidad de historial |
| `DeleteAsync` idempotente (find+remove+save en repo) | El handler de Remove siempre devuelve éxito; encapsular la idempotencia en el repo simplifica el handler |
| `ExistsAsync` antes del `Add` | Evita la excepción de BD por el UNIQUE constraint sin necesitar try/catch |
| `.Include(w => w.Property).ThenInclude(p => p.Images)` | Un solo query carga todo lo necesario para `PropertyDto`; evita N+1 |
| Filtro `w.Property.IsActive` en el repo (no en el handler) | El repo ya conoce el contexto de "solo activos"; el handler queda agnóstico a ese detalle |
| Cascade en FKs | WishlistItems son derivados de Property y User; si el padre desaparece físicamente, el hijo debe irse también |
| Reutiliza `PropertyDto` de Feature 2 | Cohesión del DTO del catálogo; el frontend puede usar el mismo modelo de datos para listar y para favoritos |
