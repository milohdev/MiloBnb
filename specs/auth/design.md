# Feature: auth — Diseño

## Estructura de carpetas nueva

```
src/
├── Milo.Application/
│   ├── Auth/
│   │   ├── AuthResponseDto.cs
│   │   ├── Commands/
│   │   │   ├── Register/
│   │   │   │   ├── RegisterCommand.cs
│   │   │   │   ├── RegisterCommandValidator.cs
│   │   │   │   └── RegisterHandler.cs
│   │   │   └── Login/
│   │   │       ├── LoginCommand.cs
│   │   │       ├── LoginCommandValidator.cs
│   │   │       └── LoginHandler.cs
│   │   └── Queries/
│   │       └── GetCurrentUser/
│   │           ├── CurrentUserDto.cs
│   │           ├── GetCurrentUserHandler.cs
│   │           └── GetCurrentUserQuery.cs
│   └── Common/
│       └── Interfaces/
│           ├── ICurrentUserProvider.cs   ← existente, sin cambios
│           ├── IJwtTokenService.cs       ← nuevo
│           └── IPasswordService.cs       ← nuevo
├── Milo.Infraestructure/
│   └── Services/
│       ├── HttpContextCurrentUserProvider.cs   ← nuevo
│       ├── JwtTokenService.cs                  ← nuevo
│       ├── NullCurrentUserProvider.cs          ← existente, queda para MiloDbContextFactory
│       └── PasswordService.cs                  ← nuevo
└── Milo.Api/
    └── Controllers/
        └── AuthController.cs   ← nuevo
```

**Archivos modificados:**
- `Milo.Infraestructure/DependencyInjection.cs` — registros actualizados
- `Milo.Api/Program.cs` — opciones JWT Bearer actualizadas
- `Milo.Api/appsettings.json` — `ExpirationHours` → `ExpirationMinutes: 480`

**Sin nuevas migrations** — la entidad `User` no cambia.

---

## Nuevos paquetes NuGet

Ninguno. Todos los tipos necesarios ya están disponibles:
- `System.IdentityModel.Tokens.Jwt` → transitivo via `Microsoft.AspNetCore.Authentication.JwtBearer`
- `PasswordHasher<T>` → `FrameworkReference Microsoft.AspNetCore.App` en Infrastructure
- `IHttpContextAccessor` → `FrameworkReference Microsoft.AspNetCore.App` en Infrastructure

---

## Diseño de interfaces (Application)

```csharp
// Application/Common/Interfaces/IJwtTokenService.cs
public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}

// Application/Common/Interfaces/IPasswordService.cs
public interface IPasswordService
{
    string Hash(string plainText);
    bool Verify(string hashedPassword, string plainText);
}
```

**Por qué `IPasswordService` en lugar de inyectar `IPasswordHasher<User>`:**
`Application` no tiene `FrameworkReference` a `Microsoft.AspNetCore.App`, por lo que no tiene acceso a `IPasswordHasher<T>`. La abstracción propia mantiene la capa libre de dependencias de ASP.NET Core.

---

## DTOs

```csharp
// Application/Auth/AuthResponseDto.cs
public record AuthResponseDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string Token,
    DateTime ExpiresAt);

// Application/Auth/Queries/GetCurrentUser/CurrentUserDto.cs
public record CurrentUserDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsKycVerified);
```

---

## Commands y Queries

```csharp
// RegisterCommand.cs — también se usa como request body del controller
public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role) : IRequest<Result<AuthResponseDto>>;

// LoginCommand.cs
public record LoginCommand(
    string Email,
    string Password) : IRequest<Result<AuthResponseDto>>;

// GetCurrentUserQuery.cs
public record GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>;
```

---

## Validators (FluentValidation)

```csharp
// RegisterCommandValidator.cs
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role)
            .Must(r => Enum.TryParse<UserRole>(r, true, out var parsed) && parsed != UserRole.Admin)
            .WithMessage("El rol debe ser 'Guest' u 'Owner'");
    }
}

// LoginCommandValidator.cs
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
```

---

## Handlers

### RegisterHandler

```csharp
public sealed class RegisterHandler(
    MiloDbContext dbContext,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService) : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        RegisterCommand request, CancellationToken cancellationToken)
    {
        // Email único (negocio, no puede ir en validator sin DB)
        if (await dbContext.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
            return Result<AuthResponseDto>.Failure("El email ya está registrado");

        // El validator garantiza que el rol es válido y no es Admin
        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);

        var hash = passwordService.Hash(request.Password);
        var user = User.Create(request.FirstName, request.LastName, request.Email, hash, role);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = jwtTokenService.GenerateToken(user);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            user.Id, user.FirstName, user.LastName, user.Email, user.Role.ToString(), token, expiresAt));
    }
}
```

### LoginHandler

```csharp
public sealed class LoginHandler(
    MiloDbContext dbContext,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService) : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        LoginCommand request, CancellationToken cancellationToken)
    {
        // Busca usuario ignorando el soft-delete filter con IgnoreQueryFilters
        // para devolver el mismo mensaje genérico si está eliminado
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !passwordService.Verify(user.PasswordHash, request.Password))
            return Result<AuthResponseDto>.Failure("Credenciales inválidas");

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure("La cuenta está desactivada");

        var (token, expiresAt) = jwtTokenService.GenerateToken(user);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            user.Id, user.FirstName, user.LastName, user.Email, user.Role.ToString(), token, expiresAt));
    }
}
```

**Nota sobre soft delete en Login:** El query filter global excluye usuarios con `IsDeleted = true`. Un usuario eliminado no se encontrará por `FirstOrDefaultAsync` y devolverá "Credenciales inválidas" — mismo mensaje, sin revelar el estado.

### GetCurrentUserHandler

```csharp
public sealed class GetCurrentUserHandler(
    MiloDbContext dbContext,
    ICurrentUserProvider currentUser) : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    public async Task<Result<CurrentUserDto>> Handle(
        GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result<CurrentUserDto>.Failure("Usuario no autenticado");

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, cancellationToken);

        if (user is null)
            return Result<CurrentUserDto>.Failure("Usuario no encontrado");

        return Result<CurrentUserDto>.Success(new CurrentUserDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.Role.ToString(), user.IsKycVerified));
    }
}
```

---

## Infrastructure — Servicios

### PasswordService

```csharp
// Infrastructure/Services/PasswordService.cs
public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string plainText) =>
        _hasher.HashPassword(string.Empty, plainText);

    public bool Verify(string hashedPassword, string plainText) =>
        _hasher.VerifyHashedPassword(string.Empty, hashedPassword, plainText)
            != PasswordVerificationResult.Failed;
}
```

**Por qué `PasswordHasher<string>`:** `User` tiene constructor privado y no puede instanciarse sin pasar por `User.Create`. El tipo genérico no afecta el resultado de hashing del PBKDF2 predeterminado, así que los hashes son compatibles con los generados por `DbSeeder` (que usa `PasswordHasher<User>`).

### JwtTokenService

```csharp
// Infrastructure/Services/JwtTokenService.cs
public sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAt) GenerateToken(User user)
    {
        var jwt = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = int.Parse(jwt["ExpirationMinutes"] ?? "480");
        var expiresAt = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("role", user.Role.ToString()),
            new Claim("given_name", user.FirstName),
            new Claim("family_name", user.LastName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
```

**Claims usando string keys simples** (ej: `"sub"`, `"role"`) en lugar de `ClaimTypes.*` porque `Program.cs` se configurará con `MapInboundClaims = false`. El middleware JWT preservará los nombres originales del claim y `[Authorize(Roles = "Admin")]` usará el claim `"role"` directamente.

### HttpContextCurrentUserProvider

```csharp
// Infrastructure/Services/HttpContextCurrentUserProvider.cs
public sealed class HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserProvider
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email => Principal?.FindFirstValue("email");
    public string? Role => Principal?.FindFirstValue("role");
}
```

---

## Infrastructure — DependencyInjection.cs (actualización)

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, IConfiguration configuration)
{
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();
    services.AddScoped<IJwtTokenService, JwtTokenService>();
    services.AddScoped<IPasswordService, PasswordService>();
    services.AddScoped<AuditInterceptor>();
    services.AddDbContext<MiloDbContext>((sp, options) =>
        options
            .UseNpgsql(configuration.GetConnectionString("Default"))
            .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
    return services;
}
```

---

## API — Program.cs (cambio en opciones JWT Bearer)

Agregar `MapInboundClaims = false` y `RoleClaimType`/`NameClaimType` dentro de `AddJwtBearer`:

```csharp
.AddJwtBearer(options =>
{
    var jwt = builder.Configuration.GetSection("Jwt");
    options.MapInboundClaims = false;   // ← nuevo: preserva nombres de claim del JWT
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt["Issuer"],
        ValidAudience = jwt["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwt["SecretKey"]!)),
        RoleClaimType = "role",          // ← nuevo: para [Authorize(Roles = "...")]
        NameClaimType = "sub"            // ← nuevo: para User.Identity.Name
    };
});
```

---

## API — AuthController

```csharp
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status409Conflict);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status401Unauthorized);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCurrentUserQuery(), ct);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(title: result.Error, statusCode: StatusCodes.Status404NotFound);
    }
}
```

**Notas del controller:**
- Los controllers toman el `Command` directamente como `[FromBody]` — evita DTOs duplicados con campos idénticos
- `[AllowAnonymous]` en register/login sobreescribe el `RequireAuthorization()` global de `Program.cs`
- `Me` no necesita `[Authorize]` explícito (viene del RequireAuthorization global), pero puede agregarse para legibilidad
- `Problem(title, statusCode)` produce RFC 9457 ProblemDetails consistente con el resto del pipeline

---

## appsettings.json (cambio)

```diff
  "Jwt": {
-   "ExpirationHours": 24,
+   "ExpirationMinutes": 480,
    ...
  }
```

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| `IPasswordService` abstracción propia | Application no tiene `FrameworkReference` a ASP.NET Core; `IPasswordHasher<T>` no está disponible en ese proyecto |
| `PasswordHasher<string>` en PasswordService | `User` tiene constructor privado; el tipo genérico no afecta el PBKDF2 predeterminado; hashes son compatibles con los del Seeder |
| `MapInboundClaims = false` | Preserva los nombres de claim simples (`"sub"`, `"role"`, `"email"`) que el `JwtTokenService` emite; evita el mapping a URIs largas de `ClaimTypes.*` |
| `RoleClaimType = "role"` | Necesario para que `[Authorize(Roles = "Admin")]` encuentre el claim `"role"` con `MapInboundClaims = false` |
| Mensaje genérico en login | No distinguir "email no encontrado" vs "contraseña incorrecta" previene user enumeration attacks |
| Controller toma Command directamente | Evita duplicar records con campos idénticos; el Command ya es el DTO de entrada |
| `NullCurrentUserProvider` se conserva | Lo usa `MiloDbContextFactory` para migrations; no se registra en DI de producción |
| Sin nueva migration | La tabla `Users` no cambia; auth opera sobre la estructura existente |
