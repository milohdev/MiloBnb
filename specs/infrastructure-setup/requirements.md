# Feature: infrastructure-setup — Requerimientos

## Objetivo
Establecer la infraestructura base del sistema MiloBnb. Ninguna feature de negocio puede implementarse sin este cimiento.

## Alcance

### RF-01 — Contenerización
- Dockerfile multi-stage para `Milo.Api` (stage `build` con `sdk:10.0`, stage `runtime` con `aspnet:10.0`, usuario no-root)
- `docker-compose.yml` con servicios `api` (puerto 8080) y `postgres` (PostgreSQL 16, puerto 5432)
- El servicio `postgres` expone healthcheck con `pg_isready`; `api` depende de postgres con `condition: service_healthy`
- `.env.example` con todas las variables de entorno requeridas y valores de ejemplo seguros para desarrollo
- `.dockerignore` que excluye `bin/`, `obj/`, `.git/`, `logs/`

### RF-02 — Structured Logging (Serilog)
- Serilog configurado en `Program.cs` via `builder.Host.UseSerilog`
- Configuración leída de la sección `"Serilog"` en `appsettings.json`
- Sinks: consola y archivo con rolling diario (`logs/milobnb-.log`)
- Overrides de nivel en Warning para `Microsoft.AspNetCore` y `Microsoft.EntityFrameworkCore`

### RF-03 — Middleware pipeline
Orden exacto en `Program.cs`:
1. `app.UseExceptionHandler()` — Problem Details nativo (RFC 9457). **NO middleware custom.**
2. `app.UseAuthentication()`
3. `app.UseAuthorization()`
4. `app.MapControllers().RequireAuthorization()` — protección por defecto; endpoints públicos usan `[AllowAnonymous]`

### RF-04 — JWT Bearer
- `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` en `Program.cs`
- Valida: issuer, audience, lifetime y firma con clave simétrica (HMAC-SHA256)
- Parámetros desde `appsettings.json`: `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationHours`
- Sin endpoints de autenticación todavía (los provee la feature `auth`)

### RF-05 — Swagger / OpenAPI
- Swashbuckle con definición de seguridad Bearer (`SecuritySchemeType.Http`, scheme `bearer`, format `JWT`)
- Botón "Authorize" habilitado en la UI
- Swagger solo habilitado en entorno `Development`
- Eliminar `Microsoft.AspNetCore.OpenApi` del `Milo.Api.csproj` (reemplazado por Swashbuckle)

### RF-06 — Application Common
Ubicación: `src/Milo.Application/Common/`

| Artefacto | Descripción |
|---|---|
| `Result<T>` | Encapsula éxito con `Value` o fallo con `Error`. Propiedades: `IsSuccess`, `Value?`, `Error?`. Factory methods `Success(T)` y `Failure(string)`. |
| `PagedResult<T>` | `Items (IReadOnlyList<T>)`, `TotalCount`, `Page`, `PageSize`, `TotalPages` (calculado). |
| `ValidationBehavior<TRequest,TResponse>` | `IPipelineBehavior` que agrega todos los `IValidator<TRequest>` y lanza `ValidationException` con la lista de fallas. Si no hay validadores, delega directamente. |
| `LoggingBehavior<TRequest,TResponse>` | `IPipelineBehavior` que logea el nombre del request al entrar y el tiempo de ejecución en ms al salir. Usa `ILogger<>` y `Stopwatch`. |
| `ICurrentUserProvider` | Interfaz placeholder: `Guid? UserId`, `string? Email`, `string? Role`. Se reemplaza en la feature `auth`. |

### RF-07 — Domain base
Ubicación: `src/Milo.Domain/`

| Artefacto | Descripción |
|---|---|
| `ISoftDeletable` | `bool IsDeleted { get; set; }`, `DateTime? DeletedAt { get; set; }` |
| `IAuditable` | `DateTime CreatedAt { get; set; }`, `DateTime? UpdatedAt { get; set; }`, `Guid? CreatedBy { get; set; }`, `Guid? UpdatedBy { get; set; }` |
| `BaseEntity` | Clase abstracta con `Guid Id { get; protected set; }` |
| `UserRole` | Enum con valores `Admin = 1`, `Guest = 2`, `Owner = 3` |
| `User` | Hereda `BaseEntity`, implementa `IAuditable` e `ISoftDeletable`. Constructor privado. Factory method estático `Create(firstName, lastName, email, passwordHash, role)`. |

Campos de `User`: `FirstName`, `LastName`, `Email`, `PasswordHash`, `Role (UserRole)`, `IsActive (default true)`, `IsKycVerified (default false)`.

### RF-08 — Infrastructure
Ubicación: `src/Milo.Infraestructure/`

| Artefacto | Descripción |
|---|---|
| `MiloDbContext` | `DbSet<User>`. `OnModelCreating` aplica `ApplyConfigurationsFromAssembly` y query filter global para `ISoftDeletable`. Override de `SaveChangesAsync`. |
| `AuditInterceptor` | `SaveChangesInterceptor`. En `Added`: rellena `CreatedAt = UtcNow`, `CreatedBy = currentUser.UserId`. En `Modified`: rellena `UpdatedAt = UtcNow`, `UpdatedBy = currentUser.UserId`. Inyecta `ICurrentUserProvider` por constructor. |
| `UserConfiguration` | `IEntityTypeConfiguration<User>`. UNIQUE index en `Email`. Longitudes máximas. `Role` guardado como string. |
| `MiloDbContextFactory` | `IDesignTimeDbContextFactory<MiloDbContext>`. Connection string hardcoded para uso exclusivo de `dotnet ef` en desarrollo. |
| `NullCurrentUserProvider` | Implementa `ICurrentUserProvider` devolviendo `null` en todo. Placeholder hasta feature `auth`. |
| `DbSeeder` | Clase estática. Inserta usuario Admin si no existe ninguno con `Role == Admin`. Hashea la contraseña con `PasswordHasher<User>`. Credenciales desde `Admin:Email` y `Admin:Password` en `IConfiguration`. |
| `DependencyInjection` | Método de extensión `AddInfrastructure(IServiceCollection, IConfiguration)`. Registra DbContext con interceptor, `ICurrentUserProvider` y demás servicios. |

### RF-09 — Migration inicial
- Migration llamada `InitialCreate` generada con `dotnet ef migrations add`
- Crea la tabla `Users` con todos sus campos
- El seed de Admin **no va en la migration**; se ejecuta en runtime vía `DbSeeder`
- Las migrations se aplican automáticamente al iniciar la app con `context.Database.MigrateAsync()`

## Fuera de alcance
- Tests automatizados
- Endpoints de autenticación (feature `auth`)
- Redis, caching, idempotency, correlation ID, rate limiting
- Outbox pattern, KYC, notificaciones, exportación Excel
- Middleware custom de excepciones
- Lógica de negocio más allá del dominio base de `User`
