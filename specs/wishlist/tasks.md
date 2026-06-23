# Feature: wishlist — Tasks

> Implementar en este orden. `dotnet build --no-incremental --nologo` después de cada tarea.
> Firmas de referencia en `design.md`. Al terminar, ejecutar migration y verificar Swagger.

---

## T-01 — Domain: WishlistItem + IWishlistRepository

**Archivos a crear:**
- `src/Milo.Domain/Entities/WishlistItem.cs`
- `src/Milo.Domain/Repositories/IWishlistRepository.cs`

`WishlistItem` extiende `BaseEntity`, implementa **solo** `IAuditable` (sin ISoftDeletable).
Propiedades: `GuestId` (Guid), `PropertyId` (Guid).
Navigation: `public Property Property { get; private set; } = null!;`
Factory: `Create(Guid guestId, Guid propertyId)` asigna `Id = Guid.NewGuid()`.

`IWishlistRepository`:
```
Task<IReadOnlyList<WishlistItem>> GetByGuestIdAsync(Guid guestId, CancellationToken ct = default)
Task<bool> ExistsAsync(Guid guestId, Guid propertyId, CancellationToken ct = default)
void Add(WishlistItem item)
Task DeleteAsync(Guid guestId, Guid propertyId, CancellationToken ct = default)
Task SaveChangesAsync(CancellationToken ct = default)
```

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-02 — Application: AddToWishlist + RemoveFromWishlist (Commands/Handlers)

**Archivos a crear:**
- `src/Milo.Application/Wishlist/Commands/AddToWishlist/AddToWishlistCommand.cs`
- `src/Milo.Application/Wishlist/Commands/AddToWishlist/AddToWishlistHandler.cs`
- `src/Milo.Application/Wishlist/Commands/RemoveFromWishlist/RemoveFromWishlistCommand.cs`
- `src/Milo.Application/Wishlist/Commands/RemoveFromWishlist/RemoveFromWishlistHandler.cs`

`AddToWishlistCommand(Guid PropertyId)` → `Result<bool>`.

`AddToWishlistHandler` inyecta `IPropertyRepository`, `IWishlistRepository`, `ICurrentUserProvider`:
1. `propertyRepository.GetByIdAsync(PropertyId)` → null o `!IsActive` → `Result.Failure("Inmueble no disponible")` [404]
2. `wishlistRepository.ExistsAsync(currentUser.UserId!.Value, PropertyId)` → true → `Result.Success(true)` (idempotente)
3. `WishlistItem.Create(currentUser.UserId!.Value, PropertyId)`
4. `wishlistRepository.Add(item)` + `wishlistRepository.SaveChangesAsync()`
5. `Result.Success(true)`

`RemoveFromWishlistCommand(Guid PropertyId)` → `Result<bool>`.

`RemoveFromWishlistHandler` inyecta `IWishlistRepository`, `ICurrentUserProvider`:
1. `wishlistRepository.DeleteAsync(currentUser.UserId!.Value, PropertyId, ct)` (idempotente)
2. `Result.Success(true)`

Sin validators explícitos — PropertyId viene del route (guid), validado por ASP.NET Core.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-03 — Application: GetWishlistQuery/Handler

**Archivos a crear:**
- `src/Milo.Application/Wishlist/Queries/GetWishlist/GetWishlistQuery.cs`
- `src/Milo.Application/Wishlist/Queries/GetWishlist/GetWishlistHandler.cs`

`GetWishlistQuery` sin parámetros → `Result<IReadOnlyList<PropertyDto>>`.
Usa `PropertyDto` de `Milo.Application.Properties.Queries.GetProperties`.

Handler inyecta `IWishlistRepository`, `ICurrentUserProvider`:
1. `wishlistRepository.GetByGuestIdAsync(currentUser.UserId!.Value, ct)`
2. `Result.Success(items.Select(ToDto).ToList())`

`ToDto(WishlistItem w)` privado y estático: mapea `w.Property.*` a `PropertyDto` con
`w.Property.Images.Select(i => i.Url).ToList()`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-04 — Infrastructure: WishlistItemConfiguration + DbContext + WishlistRepository + DI + Migration

**Archivos a crear:**
- `src/Milo.Infraestructure/Persistence/Configurations/WishlistItemConfiguration.cs`
- `src/Milo.Infraestructure/Persistence/Repositories/WishlistRepository.cs`

**Archivos a modificar:**
- `src/Milo.Infraestructure/Persistence/MiloDbContext.cs` — agregar `DbSet<WishlistItem>`
- `src/Milo.Infraestructure/DependencyInjection.cs` — agregar `AddScoped<IWishlistRepository, WishlistRepository>()`

`WishlistItemConfiguration`:
- `HasKey(w => w.Id)`
- Índice UNIQUE: `HasIndex(w => new { w.GuestId, w.PropertyId }).IsUnique()`
- Índice: `HasIndex(w => w.GuestId)`
- FK Property: `HasOne(w => w.Property).WithMany().HasForeignKey(w => w.PropertyId).OnDelete(Cascade)`
- FK User: `HasOne<User>().WithMany().HasForeignKey(w => w.GuestId).OnDelete(Cascade)`

`WishlistRepository.GetByGuestIdAsync`: usar `.Include(w => w.Property).ThenInclude(p => p.Images)`
más `.Where(w => w.GuestId == guestId && w.Property.IsActive).OrderByDescending(w => w.CreatedAt)`.

`WishlistRepository.DeleteAsync`: `FirstOrDefaultAsync` por `(GuestId, PropertyId)` → si no null,
`dbContext.WishlistItems.Remove(item)` + `dbContext.SaveChangesAsync(ct)`. Si null, no hace nada.

**Migration:**
```bash
dotnet ef migrations add AddWishlist \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Validar migration: tabla `WishlistItems`, índice UNIQUE en `(GuestId, PropertyId)`, índice en `GuestId`,
FKs Cascade a `Properties` y `Users`.

**Checkpoint:** `dotnet build --no-incremental` en la raíz sin errores.

---

## T-05 — API: WishlistController

**Archivo a crear:**
- `src/Milo.Api/Controllers/WishlistController.cs`

`[Authorize(Roles = "Guest")]` a nivel de clase (aplica a las tres acciones).

| Método | Ruta | Respuesta |
|---|---|---|
| POST | /api/wishlist/{propertyId:guid} | 200 Ok (sin cuerpo) o 404 Problem |
| DELETE | /api/wishlist/{propertyId:guid} | 204 No Content (siempre, idempotente) |
| GET | /api/wishlist | 200 Ok con lista (puede ser vacía) |

`Add`: `result.IsSuccess ? Ok() : Problem(title: result.Error, statusCode: 404)`
`Remove`: descarta el Result, devuelve `NoContent()` directamente (siempre éxito).
`GetMy`: `Ok(result.Value)` directamente.

**Checkpoint:** `dotnet build --no-incremental` en la raíz sin errores.

---

## Verificación final

Con PostgreSQL corriendo:

- [ ] `dotnet build` en raíz: 0 errores, 0 warnings
- [ ] Migration aplicada: tabla `WishlistItems` con UNIQUE en `(GuestId, PropertyId)`
- [ ] Swagger muestra los 3 endpoints de `/api/wishlist`
- [ ] `POST /api/wishlist/{id}` sin token → 401
- [ ] `POST /api/wishlist/{id}` con token Owner → 403 (rol incorrecto)
- [ ] `POST /api/wishlist/{id}` con id de inmueble inexistente → 404
- [ ] `POST /api/wishlist/{id}` con token Guest + inmueble activo → 200
- [ ] `POST /api/wishlist/{id}` misma petición de nuevo → 200 (idempotente, sin error)
- [ ] `GET /api/wishlist` → 200 con lista que incluye el inmueble guardado (con imágenes)
- [ ] `DELETE /api/wishlist/{id}` → 204
- [ ] `DELETE /api/wishlist/{id}` de nuevo → 204 (idempotente, sin error)
- [ ] `GET /api/wishlist` → 200 con lista vacía
- [ ] Actualizar `progress/current.md`
