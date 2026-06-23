# Feature: auth — Tasks

> Implementar en este orden. Ejecutar `dotnet build` al terminar cada tarea.
> Las firmas de referencia están en `design.md`. No hay nuevas migrations.

---

## T-01 — Application: interfaces comunes + DTOs de respuesta

**Archivos a crear:**
- `src/Milo.Application/Common/Interfaces/IJwtTokenService.cs`
- `src/Milo.Application/Common/Interfaces/IPasswordService.cs`
- `src/Milo.Application/Auth/AuthResponseDto.cs`
- `src/Milo.Application/Auth/Queries/GetCurrentUser/CurrentUserDto.cs`

`IJwtTokenService` retorna `(string Token, DateTime ExpiresAt) GenerateToken(User user)`.
`IPasswordService` tiene `string Hash(string plainText)` y `bool Verify(string hashedPassword, string plainText)`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-02 — Application: RegisterCommand + RegisterCommandValidator + RegisterHandler

**Archivos a crear:**
- `src/Milo.Application/Auth/Commands/Register/RegisterCommand.cs`
- `src/Milo.Application/Auth/Commands/Register/RegisterCommandValidator.cs`
- `src/Milo.Application/Auth/Commands/Register/RegisterHandler.cs`

`RegisterCommand`: record con `FirstName`, `LastName`, `Email`, `Password`, `Role` que implementa `IRequest<Result<AuthResponseDto>>`.

Validator: `FirstName`/`LastName` notEmpty+maxLen 100; `Email` notEmpty+emailAddress+maxLen 256; `Password` notEmpty+minLen 8; `Role` debe ser `"Guest"` o `"Owner"` (case-insensitive, no `"Admin"`).

Handler inyecta `MiloDbContext`, `IPasswordService`, `IJwtTokenService`. Verifica email único con `AnyAsync`; si falla retorna `Result.Failure("El email ya está registrado")`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-03 — Application: LoginCommand + LoginCommandValidator + LoginHandler

**Archivos a crear:**
- `src/Milo.Application/Auth/Commands/Login/LoginCommand.cs`
- `src/Milo.Application/Auth/Commands/Login/LoginCommandValidator.cs`
- `src/Milo.Application/Auth/Commands/Login/LoginHandler.cs`

`LoginCommand`: record con `Email`, `Password` que implementa `IRequest<Result<AuthResponseDto>>`.

Validator mínimo: `Email` notEmpty+emailAddress; `Password` notEmpty.

Handler:
1. Busca usuario por email (el query filter excluye soft-deleted → devuelve null si está eliminado)
2. Si null o contraseña no verificada → `Result.Failure("Credenciales inválidas")`
3. Si `!user.IsActive` → `Result.Failure("La cuenta está desactivada")`
4. Genera token y retorna `Result.Success(AuthResponseDto)`

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-04 — Application: GetCurrentUserQuery + GetCurrentUserHandler

**Archivos a crear:**
- `src/Milo.Application/Auth/Queries/GetCurrentUser/GetCurrentUserQuery.cs`
- `src/Milo.Application/Auth/Queries/GetCurrentUser/GetCurrentUserHandler.cs`

`GetCurrentUserQuery`: record sin propiedades que implementa `IRequest<Result<CurrentUserDto>>`.

Handler inyecta `MiloDbContext` e `ICurrentUserProvider`. Si `currentUser.UserId` es null → `Result.Failure("Usuario no autenticado")`. Busca por Id; si null → `Result.Failure("Usuario no encontrado")`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj --no-incremental` sin errores.

---

## T-05 — Infrastructure: PasswordService + JwtTokenService + HttpContextCurrentUserProvider

**Archivos a crear:**
- `src/Milo.Infraestructure/Services/PasswordService.cs`
- `src/Milo.Infraestructure/Services/JwtTokenService.cs`
- `src/Milo.Infraestructure/Services/HttpContextCurrentUserProvider.cs`

`PasswordService` implementa `IPasswordService` usando `PasswordHasher<string>` con `string.Empty` como contexto de usuario.

`JwtTokenService` implementa `IJwtTokenService`. Inyecta `IConfiguration`. Lee `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes`. Genera claims: `"sub"`, `"email"`, `"role"`, `"given_name"`, `"family_name"`, `"jti"`. Retorna `(tokenString, expiresAt)`.

`HttpContextCurrentUserProvider` implementa `ICurrentUserProvider`. Inyecta `IHttpContextAccessor`. Lee `Principal.FindFirstValue("sub")` para `UserId` (parsear como Guid), `"email"` y `"role"`.

**Checkpoint:** `dotnet build src/Milo.Infraestructure/Milo.Infraestructure.csproj --no-incremental` sin errores.

---

## T-06 — Infrastructure DI + Program.cs + appsettings.json

### Milo.Infraestructure/DependencyInjection.cs (reemplazar completo)

Agregar:
- `services.AddHttpContextAccessor()`
- `services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>()` (reemplaza `NullCurrentUserProvider`)
- `services.AddScoped<IJwtTokenService, JwtTokenService>()`
- `services.AddScoped<IPasswordService, PasswordService>()`

Mantener: `AuditInterceptor`, `AddDbContext`.

### Milo.Api/Program.cs (modificar solo el bloque AddJwtBearer)

Dentro de `.AddJwtBearer(options => { ... })` agregar:
```csharp
options.MapInboundClaims = false;
```
Y dentro de `TokenValidationParameters` agregar:
```csharp
RoleClaimType = "role",
NameClaimType = "sub"
```

### Milo.Api/appsettings.json

Cambiar `"ExpirationHours": 24` por `"ExpirationMinutes": 480`.

**Checkpoint:** `dotnet build --no-incremental` en la raíz sin errores.

---

## T-07 — API: AuthController

**Archivos a crear:**
- `src/Milo.Api/Controllers/AuthController.cs`

Atributos del controller: `[ApiController]`, `[Route("api/auth")]`.

Inyectar `IMediator` por constructor primario.

Acciones:
| Método | Ruta | Atributos | Mapeo de error |
|---|---|---|---|
| `Register(RegisterCommand, CancellationToken)` | POST `/api/auth/register` | `[AllowAnonymous]` | `Result.IsSuccess ? Ok : Problem(409)` |
| `Login(LoginCommand, CancellationToken)` | POST `/api/auth/login` | `[AllowAnonymous]` | `Result.IsSuccess ? Ok : Problem(401)` |
| `Me(CancellationToken)` | GET `/api/auth/me` | _(hereda global)_ | `Result.IsSuccess ? Ok : Problem(404)` |

Usar `Problem(title: result.Error, statusCode: StatusCodes.Status4xxY)` para los errores, que produce RFC 9457.

**Checkpoint:** `dotnet build --no-incremental` en la raíz sin errores.

---

## Verificación final

Al terminar todas las tasks y con PostgreSQL corriendo:

- [ ] `dotnet build` en la raíz: 0 errores, 0 warnings
- [ ] `POST /api/auth/register` con rol `"Guest"` → 200 + token JWT
- [ ] `POST /api/auth/register` con mismo email → 409 Conflict
- [ ] `POST /api/auth/register` con rol `"Admin"` → 400 Bad Request
- [ ] `POST /api/auth/login` con credenciales del Admin seed → 200 + token JWT
- [ ] `POST /api/auth/login` con contraseña incorrecta → 401 Unauthorized
- [ ] `GET /api/auth/me` con token válido en Swagger → 200 + datos del usuario
- [ ] `GET /api/auth/me` sin token → 401 (rechazado por middleware JWT, no llega al controller)
- [ ] Token decodificado en jwt.io tiene claims `sub`, `email`, `role`, `given_name`, `family_name`
- [ ] Actualizar `progress/current.md`
