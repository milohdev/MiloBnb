# Feature: properties — Design

## Nuevos paquetes NuGet
Ninguno. Todo el stack necesario ya está disponible.

---

## Estructura de carpetas nueva

```
src/
├── Milo.Domain/
│   ├── Entities/
│   │   ├── Property.cs                          ← nueva
│   │   └── PropertyImage.cs                     ← nueva
│   └── Repositories/
│       ├── IPropertyRepository.cs               ← nueva
│       └── IPropertyImageRepository.cs          ← nueva
│
├── Milo.Application/
│   └── Properties/
│       ├── Commands/
│       │   ├── CreateProperty/
│       │   │   ├── CreatePropertyCommand.cs
│       │   │   ├── CreatePropertyCommandValidator.cs
│       │   │   └── CreatePropertyHandler.cs
│       │   ├── UpdateProperty/
│       │   │   ├── UpdatePropertyCommand.cs
│       │   │   ├── UpdatePropertyCommandValidator.cs
│       │   │   └── UpdatePropertyHandler.cs
│       │   ├── DeleteProperty/
│       │   │   ├── DeletePropertyCommand.cs
│       │   │   └── DeletePropertyHandler.cs
│       │   ├── AddPropertyImage/
│       │   │   ├── AddPropertyImageCommand.cs
│       │   │   ├── AddPropertyImageCommandValidator.cs
│       │   │   └── AddPropertyImageHandler.cs
│       │   └── DeletePropertyImage/
│       │       ├── DeletePropertyImageCommand.cs
│       │       └── DeletePropertyImageHandler.cs
│       └── Queries/
│           ├── GetProperties/
│           │   ├── GetPropertiesQuery.cs
│           │   ├── GetPropertiesQueryValidator.cs
│           │   ├── GetPropertiesHandler.cs
│           │   └── PropertyDto.cs               ← compartido con GetById
│           └── GetPropertyById/
│               ├── GetPropertyByIdQuery.cs
│               └── GetPropertyByIdHandler.cs
│
├── Milo.Infraestructure/
│   └── Persistence/
│       ├── Configurations/
│       │   ├── PropertyConfiguration.cs         ← nueva
│       │   └── PropertyImageConfiguration.cs    ← nueva
│       └── Repositories/
│           ├── PropertyRepository.cs            ← nueva
│           └── PropertyImageRepository.cs       ← nueva
│
└── Milo.Api/
    └── Controllers/
        └── PropertiesController.cs              ← nueva
```

---

## Domain — Entidades

### `Property.cs`

```csharp
namespace Milo.Domain.Entities;

public sealed class Property : BaseEntity, IAuditable, ISoftDeletable
{
    private Property() { }

    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public decimal PricePerNight { get; private set; }
    public int MaxGuests { get; private set; }
    public int Bedrooms { get; private set; }
    public int Bathrooms { get; private set; }
    public bool AllowSameDayBooking { get; private set; }
    public bool IsActive { get; private set; }
    public Guid OwnerId { get; private set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navegación EF (cargada explícitamente con .Include)
    public ICollection<PropertyImage> Images { get; private set; } = [];

    public static Property Create(
        string name, string description, string address, string city, string country,
        decimal pricePerNight, int maxGuests, int bedrooms, int bathrooms,
        bool allowSameDayBooking, Guid ownerId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Address = address,
            City = city,
            Country = country,
            PricePerNight = pricePerNight,
            MaxGuests = maxGuests,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            AllowSameDayBooking = allowSameDayBooking,
            IsActive = true,
            OwnerId = ownerId
        };

    public void Update(
        string name, string description, string address, string city, string country,
        decimal pricePerNight, int maxGuests, int bedrooms, int bathrooms,
        bool allowSameDayBooking)
    {
        Name = name;
        Description = description;
        Address = address;
        City = city;
        Country = country;
        PricePerNight = pricePerNight;
        MaxGuests = maxGuests;
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        AllowSameDayBooking = allowSameDayBooking;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
```

### `PropertyImage.cs`

```csharp
namespace Milo.Domain.Entities;

public sealed class PropertyImage : BaseEntity
{
    private PropertyImage() { }

    public Guid PropertyId { get; private set; }
    public string Url { get; private set; } = default!;

    public static PropertyImage Create(Guid propertyId, string url) =>
        new() { Id = Guid.NewGuid(), PropertyId = propertyId, Url = url };
}
```

`PropertyImage` no implementa IAuditable ni ISoftDeletable (delete físico, sin auditoría).

---

## Domain — Repositorios

### `IPropertyRepository.cs`

```csharp
namespace Milo.Domain.Repositories;

public interface IPropertyRepository
{
    Task<Property?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Property> Items, int TotalCount)> GetAllAsync(
        string? city, DateOnly? checkIn, DateOnly? checkOut, int? maxGuests,
        int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Property>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    void Add(Property property);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`GetByIdAsync` y `GetAllAsync` deben cargar `Images` con `.Include`. Los métodos que modifican
(Update/Delete) simplemente modifican la entidad y llaman `SaveChangesAsync` (EF trackea los cambios).

### `IPropertyImageRepository.cs`

```csharp
namespace Milo.Domain.Repositories;

public interface IPropertyImageRepository
{
    Task<PropertyImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PropertyImage>> GetByPropertyIdAsync(Guid propertyId, CancellationToken cancellationToken = default);
    void Add(PropertyImage image);
    void Remove(PropertyImage image);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## Application — DTO

### `PropertyDto.cs` (en `Properties/Queries/GetProperties/`)

```csharp
namespace Milo.Application.Properties.Queries.GetProperties;

public record PropertyDto(
    Guid Id,
    string Name,
    string Description,
    string Address,
    string City,
    string Country,
    decimal PricePerNight,
    int MaxGuests,
    int Bedrooms,
    int Bathrooms,
    bool AllowSameDayBooking,
    bool IsActive,
    Guid OwnerId,
    IReadOnlyList<string> Images,
    DateTime CreatedAt);
```

Mapeo auxiliar (método privado estático en cada handler que lo necesite, NO extension method):

```csharp
private static PropertyDto ToDto(Property p) =>
    new(p.Id, p.Name, p.Description, p.Address, p.City, p.Country,
        p.PricePerNight, p.MaxGuests, p.Bedrooms, p.Bathrooms,
        p.AllowSameDayBooking, p.IsActive, p.OwnerId,
        p.Images.Select(i => i.Url).ToList(),
        p.CreatedAt);
```

---

## Application — Commands

### `CreatePropertyCommand.cs`

```csharp
public record CreatePropertyCommand(
    string Name, string Description, string Address, string City, string Country,
    decimal PricePerNight, int MaxGuests, int Bedrooms, int Bathrooms,
    bool AllowSameDayBooking) : IRequest<Result<PropertyDto>>;
```

Sin `OwnerId` en el cuerpo — el handler lo toma de `ICurrentUserProvider`.

### `CreatePropertyCommandValidator.cs`

```csharp
public sealed class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PricePerNight).GreaterThan(0);
        RuleFor(x => x.MaxGuests).InclusiveBetween(1, 20);
        RuleFor(x => x.Bedrooms).InclusiveBetween(0, 20);
        RuleFor(x => x.Bathrooms).InclusiveBetween(0, 20);
    }
}
```

### `CreatePropertyHandler.cs`

```csharp
public sealed class CreatePropertyHandler(
    IPropertyRepository repository,
    ICurrentUserProvider currentUser) : IRequestHandler<CreatePropertyCommand, Result<PropertyDto>>
{
    public async Task<Result<PropertyDto>> Handle(
        CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        // currentUser.UserId no puede ser null aquí: [Authorize(Roles = "Owner")] garantiza JWT válido
        var property = Property.Create(
            request.Name, request.Description, request.Address, request.City, request.Country,
            request.PricePerNight, request.MaxGuests, request.Bedrooms, request.Bathrooms,
            request.AllowSameDayBooking, currentUser.UserId!.Value);

        repository.Add(property);
        await repository.SaveChangesAsync(cancellationToken);

        return Result<PropertyDto>.Success(ToDto(property));
    }

    private static PropertyDto ToDto(Property p) => ...;
}
```

### `UpdatePropertyCommand.cs`

```csharp
public record UpdatePropertyCommand(
    Guid PropertyId,   // del route
    string Name, string Description, string Address, string City, string Country,
    decimal PricePerNight, int MaxGuests, int Bedrooms, int Bathrooms,
    bool AllowSameDayBooking) : IRequest<Result<PropertyDto>>;
```

### `UpdatePropertyCommandValidator.cs`

Mismas reglas que `CreatePropertyCommandValidator` más:
```csharp
RuleFor(x => x.PropertyId).NotEmpty();
```

### `UpdatePropertyHandler.cs`

```csharp
// Inyecta: IPropertyRepository, ICurrentUserProvider
// 1. GetByIdAsync → null → Result.Failure("Inmueble no encontrado") [404 en controller]
// 2. property.OwnerId != currentUser.UserId → Result.Failure("No tienes permiso para editar este inmueble") [403 en controller]
// 3. property.Update(...)
// 4. SaveChangesAsync
// 5. Result.Success(ToDto(property))
```

### `DeletePropertyCommand.cs`

```csharp
public record DeletePropertyCommand(Guid PropertyId) : IRequest<Result<bool>>;
```

### `DeletePropertyHandler.cs`

```csharp
// Inyecta: IPropertyRepository, ICurrentUserProvider
// 1. GetByIdAsync → null → Result.Failure("Inmueble no encontrado") [404]
// 2. OwnerId check → Result.Failure("No tienes permiso") [403]
// 3. property.SoftDelete()
// 4. SaveChangesAsync
// 5. Result.Success(true)
```

### `AddPropertyImageCommand.cs`

```csharp
public record AddPropertyImageCommand(Guid PropertyId, string Url) : IRequest<Result<Guid>>;
```

### `AddPropertyImageCommandValidator.cs`

```csharp
RuleFor(x => x.PropertyId).NotEmpty();
RuleFor(x => x.Url).NotEmpty().MaximumLength(2048).Must(u => Uri.TryCreate(u, UriKind.Absolute, out _))
    .WithMessage("La URL no es válida");
```

### `AddPropertyImageHandler.cs`

```csharp
// Inyecta: IPropertyRepository, IPropertyImageRepository, ICurrentUserProvider
// 1. GetByIdAsync (sin Include de imágenes, solo verificar existencia y OwnerId)
// 2. OwnerId check
// 3. PropertyImage.Create(propertyId, url)
// 4. imageRepo.Add(image)
// 5. imageRepo.SaveChangesAsync
// 6. Result.Success(image.Id)
```

Nota: `IPropertyRepository.GetByIdAsync` carga imágenes; para este handler solo se necesita verificar
existencia y OwnerId. Se usa igualmente (rendimiento irrelevante en escrituras).

### `DeletePropertyImageCommand.cs`

```csharp
public record DeletePropertyImageCommand(Guid PropertyId, Guid ImageId) : IRequest<Result<bool>>;
```

### `DeletePropertyImageHandler.cs`

```csharp
// Inyecta: IPropertyRepository, IPropertyImageRepository, ICurrentUserProvider
// 1. GetByIdAsync → null → Result.Failure [404]
// 2. OwnerId check [403]
// 3. imageRepo.GetByIdAsync → null → Result.Failure("Imagen no encontrada") [404]
// 4. Verificar image.PropertyId == request.PropertyId (evita borrar imagen ajena)
// 5. imageRepo.Remove(image)
// 6. imageRepo.SaveChangesAsync
// 7. Result.Success(true)
```

---

## Application — Queries

### `GetPropertiesQuery.cs`

```csharp
public record GetPropertiesQuery(
    string? City,
    DateOnly? CheckIn,
    DateOnly? CheckOut,
    int? MaxGuests,
    int Page = 1,
    int PageSize = 10) : IRequest<Result<PagedResult<PropertyDto>>>;
```

### `GetPropertiesQueryValidator.cs`

```csharp
RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
RuleFor(x => x.MaxGuests).GreaterThanOrEqualTo(1).When(x => x.MaxGuests.HasValue);
// Si se proveen fechas, ambas deben estar presentes y CheckOut > CheckIn
When(x => x.CheckIn.HasValue || x.CheckOut.HasValue, () =>
{
    RuleFor(x => x.CheckIn).NotNull().WithMessage("checkIn requerido si se especifica checkOut");
    RuleFor(x => x.CheckOut).NotNull().WithMessage("checkOut requerido si se especifica checkIn");
    RuleFor(x => x.CheckOut)
        .GreaterThan(x => x.CheckIn!.Value)
        .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
        .WithMessage("checkOut debe ser posterior a checkIn");
});
```

### `GetPropertiesHandler.cs`

```csharp
// Inyecta: IPropertyRepository
// 1. repository.GetAllAsync(...) → (items, totalCount)
// 2. new PagedResult<PropertyDto> { Items = items.Select(ToDto), TotalCount, Page, PageSize }
// 3. Result.Success(pagedResult)
// Nota: checkIn/checkOut se pasan al repo pero el repo los ignora (TODO: implementar en Feature 3)
```

### `GetPropertyByIdQuery.cs`

```csharp
public record GetPropertyByIdQuery(Guid PropertyId) : IRequest<Result<PropertyDto>>;
```

### `GetPropertyByIdHandler.cs`

```csharp
// Inyecta: IPropertyRepository
// 1. GetByIdAsync → null → Result.Failure("Inmueble no encontrado") [404]
// 2. Result.Success(ToDto(property))
```

---

## Infrastructure — EF Configurations

### `PropertyConfiguration.cs`

```csharp
public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.Address).HasMaxLength(200).IsRequired();
        builder.Property(p => p.City).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Country).HasMaxLength(200).IsRequired();
        builder.Property(p => p.PricePerNight).HasPrecision(18, 2).IsRequired();
        builder.HasIndex(p => p.City);   // acelera filtro por ciudad
        builder.HasIndex(p => p.OwnerId);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(p => p.OwnerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Images)
               .WithOne()
               .HasForeignKey(i => i.PropertyId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### `PropertyImageConfiguration.cs`

```csharp
public sealed class PropertyImageConfiguration : IEntityTypeConfiguration<PropertyImage>
{
    public void Configure(EntityTypeBuilder<PropertyImage> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Url).HasMaxLength(2048).IsRequired();
    }
}
```

No se configura FK aquí (ya está en `PropertyConfiguration` con `HasMany/WithOne`).

---

## Infrastructure — MiloDbContext (modificación)

Agregar los dos DbSets:

```csharp
public DbSet<Property> Properties => Set<Property>();
public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
```

El query filter global `ApplySoftDeleteFilters` ya aplica automáticamente a `Property`
(implementa ISoftDeletable). `PropertyImage` no implementa ISoftDeletable → sin filtro.

---

## Infrastructure — Repositorios

### `PropertyRepository.cs`

```csharp
public sealed class PropertyRepository(MiloDbContext dbContext) : IPropertyRepository
{
    public async Task<Property?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Properties
               .Include(p => p.Images)
               .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<(IReadOnlyList<Property> Items, int TotalCount)> GetAllAsync(
        string? city, DateOnly? checkIn, DateOnly? checkOut, int? maxGuests,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = dbContext.Properties
            .Include(p => p.Images)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(p => p.City.ToLower() == city.ToLower());

        if (maxGuests.HasValue)
            query = query.Where(p => p.MaxGuests >= maxGuests.Value);

        // TODO Feature 3: excluir propiedades con reservas solapadas entre checkIn y checkOut

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<Property>> GetByOwnerIdAsync(Guid ownerId, CancellationToken ct = default)
        => await dbContext.Properties
               .Include(p => p.Images)
               .Where(p => p.OwnerId == ownerId)
               .OrderByDescending(p => p.CreatedAt)
               .ToListAsync(ct);

    public void Add(Property property) => dbContext.Properties.Add(property);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);
}
```

### `PropertyImageRepository.cs`

```csharp
public sealed class PropertyImageRepository(MiloDbContext dbContext) : IPropertyImageRepository
{
    public async Task<PropertyImage?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.PropertyImages.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<PropertyImage>> GetByPropertyIdAsync(Guid propertyId, CancellationToken ct = default)
        => await dbContext.PropertyImages
               .Where(i => i.PropertyId == propertyId)
               .ToListAsync(ct);

    public void Add(PropertyImage image) => dbContext.PropertyImages.Add(image);

    public void Remove(PropertyImage image) => dbContext.PropertyImages.Remove(image);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => dbContext.SaveChangesAsync(ct);
}
```

---

## Infrastructure — DependencyInjection (modificación)

Agregar dos líneas dentro de `AddInfrastructure`:

```csharp
services.AddScoped<IPropertyRepository, PropertyRepository>();
services.AddScoped<IPropertyImageRepository, PropertyImageRepository>();
```

---

## Infrastructure — Migration

```
dotnet ef migrations add AddProperties --project src/Milo.Infraestructure --startup-project src/Milo.Api
```

Valida que el snapshot incluya tablas `Properties` y `PropertyImages` con FKs y precision correctos.

---

## API — PropertiesController

```csharp
[ApiController]
[Route("api/properties")]
public sealed class PropertiesController(ISender sender) : ControllerBase
{
    // ── Público ─────────────────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetPropertiesQuery query, CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return Ok(result.Value);   // ValidationBehavior ya rechaza 400; GetAll nunca falla
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetPropertyByIdQuery(id), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    // ── Owner ────────────────────────────────────────────────────────────────

    [Authorize(Roles = "Owner")]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreatePropertyCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    [Authorize(Roles = "Owner")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdatePropertyBodyDto body, CancellationToken ct)
    {
        var cmd = new UpdatePropertyCommand(id, body.Name, body.Description, body.Address,
            body.City, body.Country, body.PricePerNight, body.MaxGuests,
            body.Bedrooms, body.Bathrooms, body.AllowSameDayBooking);
        var result = await sender.Send(cmd, ct);
        return result.IsSuccess ? Ok(result.Value) : MapError(result.Error!);
    }

    [Authorize(Roles = "Owner")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeletePropertyCommand(id), ct);
        return result.IsSuccess ? NoContent() : MapError(result.Error!);
    }

    [Authorize(Roles = "Owner")]
    [HttpPost("{id:guid}/images")]
    public async Task<IActionResult> AddImage(
        Guid id, [FromBody] AddImageBodyDto body, CancellationToken ct)
    {
        var result = await sender.Send(new AddPropertyImageCommand(id, body.Url), ct);
        return result.IsSuccess
            ? Created(string.Empty, new { imageId = result.Value })
            : MapError(result.Error!);
    }

    [Authorize(Roles = "Owner")]
    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken ct)
    {
        var result = await sender.Send(new DeletePropertyImageCommand(id, imageId), ct);
        return result.IsSuccess ? NoContent() : MapError(result.Error!);
    }

    // Helper: mapea errores de negocio al status HTTP correcto según mensaje
    private IActionResult MapError(string error)
    {
        if (error.Contains("permiso", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status403Forbidden);
        if (error.Contains("no encontrad", StringComparison.OrdinalIgnoreCase))
            return Problem(title: error, statusCode: StatusCodes.Status404NotFound);
        return Problem(title: error, statusCode: StatusCodes.Status400BadRequest);
    }
}

// DTOs de cuerpo para rutas con param de ruta + body
public record UpdatePropertyBodyDto(
    string Name, string Description, string Address, string City, string Country,
    decimal PricePerNight, int MaxGuests, int Bedrooms, int Bathrooms, bool AllowSameDayBooking);

public record AddImageBodyDto(string Url);
```

Los dos records `UpdatePropertyBodyDto` y `AddImageBodyDto` van en el mismo archivo del controller
(al final, fuera de la clase). Son DTOs de binding HTTP, no de dominio.

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| `OwnerId` no va en CreatePropertyCommand | Evita que el cliente envíe un OwnerId diferente al suyo; se toma siempre del JWT |
| `SoftDelete()` método en entidad | Encapsula la lógica de borrado en el dominio; el handler solo llama el método y guarda |
| Filtro de ciudad con `ToLower()` en EF | `EF.Functions.ILike` es Npgsql-específico; `ToLower()` genera `LOWER()` portable |
| `TODO` comentado en filtro de fechas | La feature 3 implementará el JOIN con Reservations; no se rompe la API actual |
| `MapError` en controller | Evita repetir la lógica de mapeo de errores en cada acción; los mensajes de error son deterministas |
| PropertyImageConfiguration no define FK | Ya está declarada en PropertyConfiguration con `HasMany/WithOne`; duplicar causa excepción EF |
| `HasIndex(City)` | El filtro más común en el catálogo; acelera búsquedas sin costo de escritura significativo |
