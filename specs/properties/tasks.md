# Feature: properties — Tasks

> Implementar en este orden. `dotnet build --no-incremental --nologo` después de cada tarea.
> Firmas de referencia en `design.md`. Al terminar todas, ejecutar migration y verificar Swagger.

---

## T-01 — Domain: entidades Property y PropertyImage

**Archivos a crear:**
- `src/Milo.Domain/Entities/Property.cs`
- `src/Milo.Domain/Entities/PropertyImage.cs`

`Property` extiende `BaseEntity`, implementa `IAuditable` e `ISoftDeletable`. Campos en diseño.
Métodos: `Create(...)` factory estático, `Update(...)` instancia, `SoftDelete()` instancia.
Propiedad de navegación: `public ICollection<PropertyImage> Images { get; private set; } = [];`

`PropertyImage` extiende solo `BaseEntity`. Sin IAuditable ni ISoftDeletable.
Factory: `Create(Guid propertyId, string url)`.

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-02 — Domain: interfaces de repositorio

**Archivos a crear:**
- `src/Milo.Domain/Repositories/IPropertyRepository.cs`
- `src/Milo.Domain/Repositories/IPropertyImageRepository.cs`

`IPropertyRepository`:
```
Task<Property?> GetByIdAsync(Guid id, CancellationToken ct = default)
Task<(IReadOnlyList<Property> Items, int TotalCount)> GetAllAsync(
    string? city, DateOnly? checkIn, DateOnly? checkOut, int? maxGuests,
    int page, int pageSize, CancellationToken ct = default)
Task<IReadOnlyList<Property>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default)
void Add(Property property)
Task SaveChangesAsync(CancellationToken ct = default)
```

`IPropertyImageRepository`:
```
Task<PropertyImage?> GetByIdAsync(Guid id, CancellationToken ct = default)
Task<IReadOnlyList<PropertyImage>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default)
void Add(PropertyImage image)
void Remove(PropertyImage image)
Task SaveChangesAsync(CancellationToken ct = default)
```

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-03 — Application: PropertyDto + CreateProperty (Command/Validator/Handler)

**Archivos a crear:**
- `src/Milo.Application/Properties/Queries/GetProperties/PropertyDto.cs`
- `src/Milo.Application/Properties/Commands/CreateProperty/CreatePropertyCommand.cs`
- `src/Milo.Application/Properties/Commands/CreateProperty/CreatePropertyCommandValidator.cs`
- `src/Milo.Application/Properties/Commands/CreateProperty/CreatePropertyHandler.cs`

`CreatePropertyCommand` no incluye `OwnerId` (handler lo obtiene de `ICurrentUserProvider`).
Handler inyecta `IPropertyRepository` e `ICurrentUserProvider`.
Cada handler que mapea a DTO define un método privado estático `ToDto(Property p)`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-04 — Application: UpdateProperty + DeleteProperty (Command/Validator/Handler)

**Archivos a crear:**
- `src/Milo.Application/Properties/Commands/UpdateProperty/UpdatePropertyCommand.cs`
- `src/Milo.Application/Properties/Commands/UpdateProperty/UpdatePropertyCommandValidator.cs`
- `src/Milo.Application/Properties/Commands/UpdateProperty/UpdatePropertyHandler.cs`
- `src/Milo.Application/Properties/Commands/DeleteProperty/DeletePropertyCommand.cs`
- `src/Milo.Application/Properties/Commands/DeleteProperty/DeletePropertyHandler.cs`

`UpdatePropertyCommand` incluye `Guid PropertyId` como primer campo (viene del route en el controller).
Validator de Update: mismas reglas que Create más `RuleFor(x => x.PropertyId).NotEmpty()`.

En ambos handlers, verificar OwnerId == currentUser.UserId; si falla:
`Result.Failure("No tienes permiso para modificar este inmueble")` (el controller lo mapea a 403).

Handler de Delete: llama `property.SoftDelete()` → `repository.SaveChangesAsync()`.
Handler de Update: llama `property.Update(...)` → `repository.SaveChangesAsync()`.
EF Core trackea el entity modificado automáticamente (no hace falta marcar estado).

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-05 — Application: AddPropertyImage + DeletePropertyImage (Command/Validator/Handler)

**Archivos a crear:**
- `src/Milo.Application/Properties/Commands/AddPropertyImage/AddPropertyImageCommand.cs`
- `src/Milo.Application/Properties/Commands/AddPropertyImage/AddPropertyImageCommandValidator.cs`
- `src/Milo.Application/Properties/Commands/AddPropertyImage/AddPropertyImageHandler.cs`
- `src/Milo.Application/Properties/Commands/DeletePropertyImage/DeletePropertyImageCommand.cs`
- `src/Milo.Application/Properties/Commands/DeletePropertyImage/DeletePropertyImageHandler.cs`

`AddPropertyImageCommand(Guid PropertyId, string Url)`.
Validator de Url: `NotEmpty()`, `MaximumLength(2048)`, `Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))`.

`AddPropertyImageHandler` inyecta `IPropertyRepository`, `IPropertyImageRepository`, `ICurrentUserProvider`.
Flujo: verificar propiedad → OwnerId check → `PropertyImage.Create(...)` → `imageRepo.Add` → `imageRepo.SaveChangesAsync` → `Result.Success(image.Id)`.

`DeletePropertyImageHandler`: verificar propiedad → OwnerId → verificar imagen → `image.PropertyId == PropertyId` → `imageRepo.Remove` → `imageRepo.SaveChangesAsync` → `Result.Success(true)`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-06 — Application: GetProperties + GetPropertyById (Query/Validator/Handler)

**Archivos a crear:**
- `src/Milo.Application/Properties/Queries/GetProperties/GetPropertiesQuery.cs`
- `src/Milo.Application/Properties/Queries/GetProperties/GetPropertiesQueryValidator.cs`
- `src/Milo.Application/Properties/Queries/GetProperties/GetPropertiesHandler.cs`
- `src/Milo.Application/Properties/Queries/GetPropertyById/GetPropertyByIdQuery.cs`
- `src/Milo.Application/Properties/Queries/GetPropertyById/GetPropertyByIdHandler.cs`

`GetPropertiesQuery` con defaults: `int Page = 1, int PageSize = 10`. Todos los filtros opcionales.
Validator: `Page >= 1`, `PageSize` entre 1 y 50, `MaxGuests >= 1` (when present), regla de fechas (ver design.md).

Handler de GetAll: pasa todos los parámetros a `repository.GetAllAsync(...)`, construye `PagedResult<PropertyDto>`.
Handler de GetById: `GetByIdAsync` → null → `Result.Failure("Inmueble no encontrado")`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-07 — Infrastructure: EF Configurations + DbContext

**Archivos a crear:**
- `src/Milo.Infraestructure/Persistence/Configurations/PropertyConfiguration.cs`
- `src/Milo.Infraestructure/Persistence/Configurations/PropertyImageConfiguration.cs`

**Archivos a modificar:**
- `src/Milo.Infraestructure/Persistence/MiloDbContext.cs` — agregar dos DbSets

`PropertyConfiguration`:
- `PricePerNight`: `HasPrecision(18, 2)`
- `HasIndex(City)`, `HasIndex(OwnerId)`
- FK a User: `HasOne<User>().WithMany().HasForeignKey(p => p.OwnerId).OnDelete(Restrict)`
- Relación con imágenes: `HasMany(p => p.Images).WithOne().HasForeignKey(i => i.PropertyId).OnDelete(Cascade)`

`PropertyImageConfiguration`: solo configura `HasKey` y `HasMaxLength(2048)` en Url. No define FK (ya la define PropertyConfiguration).

En MiloDbContext:
```csharp
public DbSet<Property> Properties => Set<Property>();
public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
```

El soft-delete global se aplica automáticamente a Property (ISoftDeletable) vía reflexión ya existente.

**Checkpoint:** `dotnet build src/Milo.Infraestructure/Milo.Infraestructure.csproj --no-incremental` sin errores.

---

## T-08 — Infrastructure: Repositorios + DI + Migration

**Archivos a crear:**
- `src/Milo.Infraestructure/Persistence/Repositories/PropertyRepository.cs`
- `src/Milo.Infraestructure/Persistence/Repositories/PropertyImageRepository.cs`

**Archivos a modificar:**
- `src/Milo.Infraestructure/DependencyInjection.cs` — agregar dos registros `AddScoped`

`PropertyRepository.GetAllAsync`: filtrar `p.IsActive`, luego `City` (case-insensitive con `ToLower()`),
luego `MaxGuests >= value`, ignorar checkIn/checkOut con comentario `// TODO Feature 3`.
Ordenar por `CreatedAt DESC`. Paginación: `Skip((page-1)*pageSize).Take(pageSize)`.

En `DependencyInjection.AddInfrastructure`:
```csharp
services.AddScoped<IPropertyRepository, PropertyRepository>();
services.AddScoped<IPropertyImageRepository, PropertyImageRepository>();
```

**Migration:**
```bash
dotnet ef migrations add AddProperties \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Revisar que el archivo de migration generado incluya:
- Tabla `Properties` con columnas correctas y precision (18,2) en `PricePerNight`
- Tabla `PropertyImages` con FK a Properties
- Índices en `City` y `OwnerId`
- FK de Properties a Users con `RestrictDelete`
- FK de PropertyImages a Properties con `CascadeDelete`

**Checkpoint:** `dotnet build --no-incremental` en la raíz sin errores.

---

## T-09 — API: PropertiesController

**Archivo a crear:**
- `src/Milo.Api/Controllers/PropertiesController.cs`

Contiene:
- La clase `PropertiesController(ISender sender) : ControllerBase`
- Los dos records de binding en el mismo archivo fuera de la clase: `UpdatePropertyBodyDto` y `AddImageBodyDto`
- El método privado `MapError(string error)` que mapea mensajes de error a códigos HTTP (403/404/400)

Acciones y atributos:
| Método HTTP | Ruta | Atributo de auth |
|---|---|---|
| GET | /api/properties | [AllowAnonymous] |
| GET | /api/properties/{id:guid} | [AllowAnonymous] |
| POST | /api/properties | [Authorize(Roles = "Owner")] |
| PUT | /api/properties/{id:guid} | [Authorize(Roles = "Owner")] |
| DELETE | /api/properties/{id:guid} | [Authorize(Roles = "Owner")] |
| POST | /api/properties/{id:guid}/images | [Authorize(Roles = "Owner")] |
| DELETE | /api/properties/{id:guid}/images/{imageId:guid} | [Authorize(Roles = "Owner")] |

Create devuelve `CreatedAtAction(nameof(GetById), new { id = ... }, dto)`.
Delete y DeleteImage devuelven `NoContent()`.
GetAll devuelve `Ok(result.Value)` directo (ValidationBehavior maneja el 400 previo).

**Checkpoint:** `dotnet build --no-incremental` en la raíz sin errores.

---

## Verificación final

Con PostgreSQL corriendo (`dotnet run` o docker-compose up):

- [ ] `dotnet build` en raíz: 0 errores, 0 warnings
- [ ] Migration aplicada al arrancar: tablas `Properties` y `PropertyImages` creadas
- [ ] Swagger muestra los 7 endpoints de `/api/properties`
- [ ] `POST /api/properties` sin token → 401
- [ ] `POST /api/properties` con token Guest → 403
- [ ] `POST /api/properties` con token Owner + body válido → 201 + PropertyDto
- [ ] `POST /api/properties` con token Owner + Name vacío → 400 (ValidationException)
- [ ] `GET /api/properties` sin token → 200 + PagedResult (lista vacía o con datos)
- [ ] `GET /api/properties?city=Bogotá&page=1&pageSize=5` → 200 filtrado
- [ ] `GET /api/properties/{id}` con id válido → 200 + PropertyDto con imágenes
- [ ] `GET /api/properties/{id}` con id inexistente → 404
- [ ] `PUT /api/properties/{id}` con token de Owner diferente → 403
- [ ] `DELETE /api/properties/{id}` → 204 + propiedad desaparece del GET
- [ ] `POST /api/properties/{id}/images` con URL válida → 201 + imageId
- [ ] `DELETE /api/properties/{id}/images/{imageId}` → 204
- [ ] Actualizar `progress/current.md`
