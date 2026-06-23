# Feature: kyc — Tasks

> Implementar en este orden. `dotnet build --no-incremental --nologo` después de cada tarea.
> Firmas de referencia en `design.md`. Al terminar, ejecutar migration y verificar Swagger.

---

## T-01 — Domain: KycStatus + KycVerification + MarkKycVerified en User

**Archivos a crear:**
- `src/Milo.Domain/Entities/Enums/KycStatus.cs` — `Pending=1, Approved=2, Rejected=3`
- `src/Milo.Domain/Entities/KycVerification.cs`

**Archivo a modificar:**
- `src/Milo.Domain/Entities/User.cs` — agregar `public void MarkKycVerified() => IsKycVerified = true;`

`KycVerification` extiende `BaseEntity + IAuditable` (sin ISoftDeletable).
Properties: `UserId`, `ExtractedFirstName?`, `ExtractedLastName?`, `ExtractedDocumentNumber?`,
`ExtractedBirthDate (DateOnly?)`, `Status (KycStatus)`, `RejectionReason?`, `DocumentImageUrl?`.
Factory: `Create(Guid userId, string imageUrl)` → `Status = KycStatus.Pending`.
Métodos: `Approve(firstName, lastName, documentNumber, birthDate?)` y `Reject(reason)` — ambos
borran `DocumentImageUrl = null` antes de retornar.

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-02 — Domain: IKycRepository

**Archivo a crear:**
- `src/Milo.Domain/Repositories/IKycRepository.cs`

```
Task<KycVerification?> GetLatestByUserIdAsync(Guid userId, CancellationToken ct = default)
void Add(KycVerification verification)
Task SaveChangesAsync(CancellationToken ct = default)
```

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj --no-incremental` sin errores.

---

## T-03 — Application: IKycService + KycExtractionResult

**Archivo a crear:**
- `src/Milo.Application/Common/Interfaces/IKycService.cs`

Contiene tanto la interfaz como el record `KycExtractionResult` en el mismo archivo:

```csharp
public interface IKycService
{
    Task<KycExtractionResult> ExtractDocumentDataAsync(
        string imageUrl, CancellationToken cancellationToken = default);
}

public record KycExtractionResult(
    string? FirstName, string? LastName, string? DocumentNumber,
    DateOnly? BirthDate, bool IsSuccessful, string? FailureReason);
```

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-04 — Application: VerifyKyc (Command/Handler/Validator) + GetKycStatus (Query/Handler) + KycVerificationDto

**Archivos a crear:**
- `src/Milo.Application/Kyc/Commands/VerifyKyc/VerifyKycCommand.cs`
- `src/Milo.Application/Kyc/Commands/VerifyKyc/VerifyKycHandler.cs`
- `src/Milo.Application/Kyc/Commands/VerifyKyc/VerifyKycCommandValidator.cs`
- `src/Milo.Application/Kyc/Queries/GetKycStatus/GetKycStatusQuery.cs`
- `src/Milo.Application/Kyc/Queries/GetKycStatus/GetKycStatusHandler.cs`
- `src/Milo.Application/Kyc/Queries/GetKycStatus/KycVerificationDto.cs`

`VerifyKycCommand(string ImageUrl)` → `Result<KycVerificationDto>`.

`VerifyKycCommandValidator`:
```
RuleFor(x => x.ImageUrl).NotEmpty().MaximumLength(2048)
  .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u)
               && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps))
  .WithMessage("ImageUrl debe ser una URL HTTP/HTTPS válida")
```

`VerifyKycHandler` inyecta `IKycRepository, IKycService, IUserRepository, ICurrentUserProvider`:
1. `GetLatestByUserIdAsync` → si Approved → `Result.Failure("Tu identidad ya fue verificada")`
2. `KycVerification.Create(userId, request.ImageUrl)`
3. `kycService.ExtractDocumentDataAsync(request.ImageUrl, ct)`
4. Si exitoso: `verification.Approve(...)` / Si falla: `verification.Reject(...)`
5. `kycRepository.Add(verification)`
6. Si Approved: `userRepository.GetByIdAsync(userId)` → `user!.MarkKycVerified()`
7. `await kycRepository.SaveChangesAsync(ct)` — commit único (persiste KycVerification + User)
8. `Result.Success(ToDto(verification))`

`GetKycStatusHandler` inyecta `IKycRepository, ICurrentUserProvider`:
1. `GetLatestByUserIdAsync` → null → `Result.Failure("No has iniciado el proceso de verificación")`
2. `Result.Success(ToDto(verification))`

`KycVerificationDto`: `Id, UserId, Status (string), ExtractedFirstName?, ExtractedLastName?,
ExtractedDocumentNumber?, ExtractedBirthDate (DateOnly?), RejectionReason?, CreatedAt`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-05 — Infrastructure: KycVerificationConfiguration + KycRepository + ClaudeKycService + DI + appsettings

**Archivos a crear:**
- `src/Milo.Infraestructure/Persistence/Configurations/KycVerificationConfiguration.cs`
- `src/Milo.Infraestructure/Persistence/Repositories/KycRepository.cs`
- `src/Milo.Infraestructure/Services/ClaudeKycService.cs`

**Archivos a modificar:**
- `src/Milo.Infraestructure/Persistence/MiloDbContext.cs` — `DbSet<KycVerification> KycVerifications`
- `src/Milo.Infraestructure/DependencyInjection.cs` — ver abajo
- `src/Milo.Api/appsettings.json` — agregar `"Anthropic": { "ApiKey": "" }`

`KycVerificationConfiguration`:
- `HasKey(k => k.Id)`
- `HasIndex(k => k.UserId)`
- `Status.HasConversion<string>().HasMaxLength(20)`
- `ExtractedFirstName.HasMaxLength(200)`, `ExtractedLastName.HasMaxLength(200)`,
  `ExtractedDocumentNumber.HasMaxLength(100)`, `RejectionReason.HasMaxLength(1000)`,
  `DocumentImageUrl.HasMaxLength(2048)`
- `HasOne<User>().WithMany().HasForeignKey(k => k.UserId).OnDelete(Restrict)`

`KycRepository.GetLatestByUserIdAsync`:
```csharp
.Where(k => k.UserId == userId).OrderByDescending(k => k.CreatedAt).FirstOrDefaultAsync(ct)
```

`ClaudeKycService`:
- Constructor: `ClaudeKycService(HttpClient httpClient, IConfiguration configuration)`
- `_apiKey = configuration["Anthropic:ApiKey"]!`
- En `ExtractDocumentDataAsync`:
  - Construir request body como objeto anónimo con model, max_tokens, messages
  - El content del mensaje incluye la imagen como `{ type: "image", source: { type: "url", url: imageUrl } }`
  - y el text con el prompt de extracción
  - `JsonSerializer.Serialize` del body → `StringContent` con `application/json`
  - Agregar headers: `httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey)` y
    `"anthropic-version": "2023-06-01"` (solo si no están ya añadidos — mejor agregar en el constructor)
  - `await httpClient.PostAsync("https://api.anthropic.com/v1/messages", content, ct)`
  - Si la respuesta no es exitosa: retornar `KycExtractionResult` con `IsSuccessful = false`
  - Parsear `response.content[0].text` como JSON
  - Si el JSON tiene campo `"error"`: `IsSuccessful = false, FailureReason = errorValue`
  - Si tiene `firstName/lastName/documentNumber`: `IsSuccessful = true`, parsear `birthDate`
  - Envolver todo en try/catch → en excepción: `IsSuccessful = false, FailureReason = "Error al procesar el documento. Intenta nuevamente."`

**Nota sobre headers de HttpClient:** Para evitar duplicar headers en cada llamada, configúralos
en el constructor de `ClaudeKycService` sobre `httpClient.DefaultRequestHeaders` (el HttpClient
es inyectado fresh por `AddHttpClient`, así que es seguro modificar sus defaults en el constructor).

`DependencyInjection.cs`:
```csharp
services.AddHttpClient<IKycService, ClaudeKycService>();
services.AddScoped<IKycRepository, KycRepository>();
```

**Checkpoint:** `dotnet build src/Milo.Infraestructure/Milo.Infraestructure.csproj --no-incremental` sin errores.

---

## T-06 — Infrastructure: Migration

```bash
dotnet ef migrations add AddKycVerification \
  --project src/Milo.Infraestructure \
  --startup-project src/Milo.Api
```

Validar en el archivo generado:
- [ ] Tabla `KycVerifications` con columnas `Id, UserId, ExtractedFirstName, ExtractedLastName,
  ExtractedDocumentNumber, ExtractedBirthDate, Status, RejectionReason, DocumentImageUrl,
  CreatedAt, UpdatedAt, CreatedBy, UpdatedBy`
- [ ] `Status` como `varchar(20)` (no int — HasConversion\<string\>)
- [ ] `ExtractedBirthDate` como `date` (Npgsql mapea DateOnly a date)
- [ ] Índice en `UserId`
- [ ] FK a `Users` con Restrict

**Checkpoint:** `dotnet build --no-incremental --nologo` en la raíz sin errores.

---

## T-07 — API: KycController

**Archivo a crear:**
- `src/Milo.Api/Controllers/KycController.cs`

`[Authorize(Roles = "Guest")]` a nivel de clase.

| Método | Ruta | Éxito | Error |
|---|---|---|---|
| POST | /api/kyc/verify | 200 Ok + KycVerificationDto | 409 Conflict (ya Approved) o 400 (validación) |
| GET | /api/kyc/status | 200 Ok + KycVerificationDto | 404 Not Found |

`VerifyKycRequest(string ImageUrl)` — record local al final del archivo (body del POST).

**Checkpoint:** `dotnet build --no-incremental --nologo` en la raíz sin errores.

---

## Verificación final

- [ ] `dotnet build` raíz: 0 errores, 0 warnings
- [ ] Migration aplicada: tabla `KycVerifications`, `Status varchar(20)`, `ExtractedBirthDate date`
- [ ] Swagger muestra `/api/kyc/verify` (POST) y `/api/kyc/status` (GET)
- [ ] POST sin token → 401
- [ ] POST con token Owner → 403
- [ ] POST con URL inválida → 400 (FluentValidation)
- [ ] POST con URL de imagen de cédula válida → 200 con Status "Approved" (o "Rejected" si falla extracción)
- [ ] POST de nuevo con ya Approved → 409
- [ ] GET /api/kyc/status sin verificación previa → 404
- [ ] GET /api/kyc/status → 200 con el resultado
- [ ] Si Approved: GET /api/reservations (crear reserva) ya no rechaza por KYC
- [ ] `DocumentImageUrl` no aparece en ninguna respuesta de los endpoints
- [ ] Actualizar `progress/current.md`
