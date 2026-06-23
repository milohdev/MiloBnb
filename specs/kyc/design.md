# Feature: kyc — Design

## Nuevos paquetes NuGet
Ninguno. Se usa `HttpClient` nativo de .NET.

---

## Estructura de carpetas nueva

```
src/
├── Milo.Domain/
│   ├── Entities/
│   │   ├── Enums/
│   │   │   └── KycStatus.cs                                    ← nueva
│   │   └── KycVerification.cs                                  ← nueva
│   └── Repositories/
│       └── IKycRepository.cs                                   ← nueva
│
├── Milo.Application/
│   ├── Common/
│   │   └── Interfaces/
│   │       └── IKycService.cs                                  ← nueva (con KycExtractionResult)
│   └── Kyc/
│       ├── Commands/
│       │   └── VerifyKyc/
│       │       ├── VerifyKycCommand.cs
│       │       ├── VerifyKycHandler.cs
│       │       └── VerifyKycCommandValidator.cs
│       └── Queries/
│           └── GetKycStatus/
│               ├── GetKycStatusQuery.cs
│               ├── GetKycStatusHandler.cs
│               └── KycVerificationDto.cs
│
├── Milo.Infraestructure/
│   ├── Persistence/
│   │   └── Configurations/
│   │       └── KycVerificationConfiguration.cs                 ← nueva
│   ├── Persistence/
│   │   └── Repositories/
│   │       └── KycRepository.cs                               ← nueva
│   └── Services/
│       └── ClaudeKycService.cs                                ← nueva
│
└── Milo.Api/
    └── Controllers/
        └── KycController.cs                                    ← nueva
```

**Archivos modificados:**
- `Milo.Domain/Entities/User.cs` — agregar `MarkKycVerified()`
- `Milo.Infraestructure/Persistence/MiloDbContext.cs` — agregar `DbSet<KycVerification>`
- `Milo.Infraestructure/DependencyInjection.cs` — registrar `IKycRepository`, `IKycService`
- `src/Milo.Api/appsettings.json` — agregar sección `Anthropic`

---

## Domain — Enum KycStatus

### `KycStatus.cs`

```csharp
namespace Milo.Domain.Entities.Enums;

public enum KycStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}
```

---

## Domain — Entidad KycVerification

### `KycVerification.cs`

```csharp
namespace Milo.Domain.Entities;

public sealed class KycVerification : BaseEntity, IAuditable
{
    private KycVerification() { }

    public Guid UserId { get; private set; }
    public string? ExtractedFirstName { get; private set; }
    public string? ExtractedLastName { get; private set; }
    public string? ExtractedDocumentNumber { get; private set; }
    public DateOnly? ExtractedBirthDate { get; private set; }
    public KycStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? DocumentImageUrl { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static KycVerification Create(Guid userId, string imageUrl) =>
        new() { Id = Guid.NewGuid(), UserId = userId,
                Status = KycStatus.Pending, DocumentImageUrl = imageUrl };

    public void Approve(string firstName, string lastName, string documentNumber, DateOnly? birthDate)
    {
        Status = KycStatus.Approved;
        ExtractedFirstName = firstName;
        ExtractedLastName = lastName;
        ExtractedDocumentNumber = documentNumber;
        ExtractedBirthDate = birthDate;
        DocumentImageUrl = null;       // privacidad: se borra antes de persistir
    }

    public void Reject(string reason)
    {
        Status = KycStatus.Rejected;
        RejectionReason = reason;
        DocumentImageUrl = null;       // privacidad: se borra antes de persistir
    }
}
```

`KycVerification` **no implementa ISoftDeletable** — los registros son inmutables de auditoría;
permanecen para historial. No se borran físicamente.

---

## Domain — Modificación a User

Agregar a `User.cs` (el setter de `IsKycVerified` ya es `private`):

```csharp
public void MarkKycVerified() => IsKycVerified = true;
```

---

## Domain — Repositorio

### `IKycRepository.cs`

```csharp
namespace Milo.Domain.Repositories;

public interface IKycRepository
{
    Task<KycVerification?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(KycVerification verification);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`GetLatestByUserIdAsync`: retorna el registro más reciente (por `CreatedAt DESC`) para el usuario.
Un Guest puede tener múltiples registros si hubo intentos fallidos anteriores.

No hay `UpdateAsync` explícito — EF Core rastrea los cambios en la entidad cargada via `GetLatestByUserIdAsync`.
Cuando el handler modifica la entidad (Approve/Reject) y llama a `SaveChangesAsync`, EF persiste el cambio.

---

## Application — IKycService + KycExtractionResult

### `IKycService.cs`

```csharp
namespace Milo.Application.Common.Interfaces;

public interface IKycService
{
    Task<KycExtractionResult> ExtractDocumentDataAsync(
        string imageUrl, CancellationToken cancellationToken = default);
}

public record KycExtractionResult(
    string? FirstName,
    string? LastName,
    string? DocumentNumber,
    DateOnly? BirthDate,
    bool IsSuccessful,
    string? FailureReason);
```

---

## Application — Commands

### `VerifyKycCommand.cs`

```csharp
public record VerifyKycCommand(string ImageUrl) : IRequest<Result<KycVerificationDto>>;
```

### `VerifyKycCommandValidator.cs`

```csharp
RuleFor(x => x.ImageUrl)
    .NotEmpty().WithMessage("La URL de la imagen es requerida")
    .MaximumLength(2048)
    .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
                 && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps))
    .WithMessage("ImageUrl debe ser una URL HTTP/HTTPS válida");
```

### `VerifyKycHandler.cs`

Inyecta: `IKycRepository`, `IKycService`, `IUserRepository`, `ICurrentUserProvider`

```
1. var userId = currentUser.UserId!.Value
2. var existing = await kycRepository.GetLatestByUserIdAsync(userId, ct)
   → if (existing?.Status == KycStatus.Approved)
       return Result.Failure("Tu identidad ya fue verificada")   [409]
3. var verification = KycVerification.Create(userId, request.ImageUrl)
4. var extraction = await kycService.ExtractDocumentDataAsync(request.ImageUrl, ct)
5. if (extraction.IsSuccessful)
       verification.Approve(extraction.FirstName!, extraction.LastName!,
                            extraction.DocumentNumber!, extraction.BirthDate)
   else
       verification.Reject(extraction.FailureReason!)
6. kycRepository.Add(verification)
7. if (verification.Status == KycStatus.Approved)
   {
       var user = await userRepository.GetByIdAsync(userId, ct)
       user!.MarkKycVerified()
   }
8. await kycRepository.SaveChangesAsync(ct)   // persiste KycVerification + User en un commit
9. return Result.Success(ToDto(verification))
```

**Punto clave — commit único:** `IKycRepository.SaveChangesAsync` y `IUserRepository.SaveChangesAsync`
comparten la misma instancia de `MiloDbContext` (scoped). Al llamar `kycRepository.SaveChangesAsync(ct)`,
EF persiste todos los cambios rastreados: el nuevo `KycVerification` y la modificación a `User.IsKycVerified`.
Solo un round-trip a la BD.

---

## Application — Queries

### `KycVerificationDto.cs`

```csharp
public record KycVerificationDto(
    Guid Id,
    Guid UserId,
    string Status,
    string? ExtractedFirstName,
    string? ExtractedLastName,
    string? ExtractedDocumentNumber,
    DateOnly? ExtractedBirthDate,
    string? RejectionReason,
    DateTime CreatedAt);
```

### `GetKycStatusQuery.cs`

```csharp
public record GetKycStatusQuery : IRequest<Result<KycVerificationDto>>;
```

### `GetKycStatusHandler.cs`

Inyecta: `IKycRepository`, `ICurrentUserProvider`

```
1. var verification = await kycRepository.GetLatestByUserIdAsync(userId, ct)
2. if (verification is null) return Result.Failure("No has iniciado el proceso de verificación")  [404]
3. return Result.Success(ToDto(verification))
```

---

## Infrastructure — ClaudeKycService

### `ClaudeKycService.cs`

Lee `Anthropic:ApiKey` desde `IConfiguration`.

**Request a la API de Anthropic** — `POST https://api.anthropic.com/v1/messages`:

Headers:
```
x-api-key: {apiKey}
anthropic-version: 2023-06-01
content-type: application/json
```

Body:
```json
{
  "model": "claude-sonnet-4-6",
  "max_tokens": 512,
  "messages": [{
    "role": "user",
    "content": [
      {
        "type": "image",
        "source": { "type": "url", "url": "<imageUrl>" }
      },
      {
        "type": "text",
        "text": "Eres un extractor de datos de documentos de identidad. Extrae los siguientes campos: firstName, lastName, documentNumber, birthDate (formato YYYY-MM-DD o null si no está claro). Responde ÚNICAMENTE con JSON sin markdown, sin texto adicional. Si la imagen no es un documento de identidad válido o no puedes extraer los datos, responde: {\"error\": \"<razón>\"}. Formato exitoso: {\"firstName\": \"\", \"lastName\": \"\", \"documentNumber\": \"\", \"birthDate\": \"\"}"
      }
    ]
  }]
}
```

**Parsing de la respuesta:**

La respuesta de Anthropic tiene la estructura:
```json
{ "content": [{ "type": "text", "text": "<json string>" }] }
```

Flujo:
```
1. Deserializar respuesta HTTP → extraer content[0].text
2. Deserializar el text como JSON
3. Si tiene clave "error" → IsSuccessful = false, FailureReason = error value
4. Si tiene firstName, lastName, documentNumber → IsSuccessful = true
5. BirthDate: DateOnly.TryParse(birthDateStr, out var d) → d o null
6. Cualquier excepción (HTTP, JSON, timeout) → IsSuccessful = false,
   FailureReason = "Error al procesar el documento. Intenta nuevamente."
```

**Registro:**

```csharp
// En DependencyInjection.cs
builder.Services.AddHttpClient<IKycService, ClaudeKycService>();
```

`ClaudeKycService` recibe `HttpClient` por constructor (el `AddHttpClient` lo inyecta).
También recibe `IConfiguration` para leer `Anthropic:ApiKey`.

---

## Infrastructure — KycVerificationConfiguration

### `KycVerificationConfiguration.cs`

```csharp
builder.HasKey(k => k.Id);

builder.HasIndex(k => k.UserId);

builder.Property(k => k.Status)
       .HasConversion<string>()
       .HasMaxLength(20);

builder.Property(k => k.ExtractedFirstName).HasMaxLength(200);
builder.Property(k => k.ExtractedLastName).HasMaxLength(200);
builder.Property(k => k.ExtractedDocumentNumber).HasMaxLength(100);
builder.Property(k => k.RejectionReason).HasMaxLength(1000);
builder.Property(k => k.DocumentImageUrl).HasMaxLength(2048);

builder.HasOne<User>()
       .WithMany()
       .HasForeignKey(k => k.UserId)
       .OnDelete(DeleteBehavior.Restrict);
```

FK Restrict: si un User es soft-deleted, sus registros KYC permanecen para auditoría.

---

## Infrastructure — KycRepository

### `KycRepository.cs`

```csharp
public sealed class KycRepository(MiloDbContext dbContext) : IKycRepository
{
    public async Task<KycVerification?> GetLatestByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.KycVerifications
               .Where(k => k.UserId == userId)
               .OrderByDescending(k => k.CreatedAt)
               .FirstOrDefaultAsync(cancellationToken);

    public void Add(KycVerification verification)
        => dbContext.KycVerifications.Add(verification);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
```

---

## Infrastructure — DependencyInjection (modificación)

```csharp
services.AddHttpClient<IKycService, ClaudeKycService>();
services.AddScoped<IKycRepository, KycRepository>();
```

---

## Infrastructure — MiloDbContext (modificación)

```csharp
public DbSet<KycVerification> KycVerifications => Set<KycVerification>();
```

`KycVerification` no implementa ISoftDeletable → el filtro global lo omite.

---

## Infrastructure — Migration

```bash
dotnet ef migrations add AddKycVerification \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Validar en el archivo generado:
- Tabla `KycVerifications` con todas las columnas (nullable correctamente)
- `Status` como `varchar(20)`
- `ExtractedBirthDate` como `date` (DateOnly → date en Npgsql)
- Índice en `UserId`
- FK a `Users` con Restrict

---

## API — KycController

```csharp
[ApiController]
[Route("api/kyc")]
[Authorize(Roles = "Guest")]
public sealed class KycController(ISender sender) : ControllerBase
{
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(VerifyKycRequest body, CancellationToken ct)
    {
        var result = await sender.Send(new VerifyKycCommand(body.ImageUrl), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status409Conflict);
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await sender.Send(new GetKycStatusQuery(), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
    }
}
```

`VerifyKycRequest` — record local al final del archivo:
```csharp
public record VerifyKycRequest(string ImageUrl);
```

---

## Configuración — appsettings.json (modificación)

Agregar sección:
```json
"Anthropic": {
  "ApiKey": ""
}
```

La clave real se inyecta vía variable de entorno `Anthropic__ApiKey` (sin commitear).

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| `Approve()` y `Reject()` en la entidad | La entidad protege sus invariantes; borra `DocumentImageUrl` dentro del método, garantizando que nunca se persista después de procesar |
| Commit único (kycRepository.SaveChangesAsync persiste KycVerification + User) | La misma instancia de DbContext rastrea ambos cambios; evitar dos round-trips y posibles inconsistencias |
| `GetLatestByUserIdAsync` en lugar de `GetByUserIdAsync` | Nombre honesto — puede haber múltiples registros (reintentos); el más reciente es el relevante |
| `AddHttpClient<IKycService, ClaudeKycService>()` | Gestión correcta de `HttpClient` (pool de conexiones, evita socket exhaustion) |
| Status almacenado como string en BD | Legible en queries directas, consistente con Reservation.Status |
| Retornar 200 Ok en POST aunque el KYC sea Rejected | La operación de verificación fue exitosa (se creó el registro); el `Status` en el DTO informa el resultado al cliente |
| Sin no-expose de `DocumentImageUrl` en DTO | Campo de privacidad; aunque se borra en la entidad antes de persistir, el DTO no lo incluye por diseño |
