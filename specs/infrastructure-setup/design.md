# Feature: infrastructure-setup — Diseño

## Estructura de carpetas objetivo

```
/
├── Dockerfile
├── docker-compose.yml
├── .env.example
├── .dockerignore
└── src/
    ├── Milo.Domain/
    │   ├── Common/
    │   │   ├── Interfaces/
    │   │   │   ├── IAuditable.cs
    │   │   │   └── ISoftDeletable.cs
    │   │   └── BaseEntity.cs
    │   └── Entities/
    │       ├── User.cs
    │       └── Enums/
    │           └── UserRole.cs
    ├── Milo.Application/
    │   ├── Common/
    │   │   ├── Behaviors/
    │   │   │   ├── ValidationBehavior.cs
    │   │   │   └── LoggingBehavior.cs
    │   │   ├── Interfaces/
    │   │   │   └── ICurrentUserProvider.cs
    │   │   └── Models/
    │   │       ├── Result.cs
    │   │       └── PagedResult.cs
    │   └── DependencyInjection.cs
    ├── Milo.Infraestructure/
    │   ├── Persistence/
    │   │   ├── Configurations/
    │   │   │   └── UserConfiguration.cs
    │   │   ├── Interceptors/
    │   │   │   └── AuditInterceptor.cs
    │   │   ├── Migrations/       ← generado por EF CLI
    │   │   ├── Seeders/
    │   │   │   └── DbSeeder.cs
    │   │   ├── MiloDbContext.cs
    │   │   └── MiloDbContextFactory.cs
    │   ├── Services/
    │   │   └── NullCurrentUserProvider.cs
    │   └── DependencyInjection.cs
    └── Milo.Api/
        ├── Program.cs             ← reescribir completamente
        ├── appsettings.json       ← agregar secciones Jwt, Admin, Serilog
        └── appsettings.Development.json
```

Archivos a eliminar: `Milo.Domain/Class1.cs`, `Milo.Application/Class1.cs`, `Milo.Infraestructure/Class1.cs`, `Milo.Api/Milo.Api.http`.

---

## Paquetes NuGet

### Milo.Domain
Sin paquetes. Solo BCL de .NET 10.

### Milo.Application
```xml
<PackageReference Include="MediatR" Version="12.*" />
<PackageReference Include="FluentValidation" Version="11.*" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.*" />
```

### Milo.Infraestructure
```xml
<FrameworkReference Include="Microsoft.AspNetCore.App" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.*">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

`FrameworkReference` es necesario para acceder a `PasswordHasher<T>` desde un proyecto class library (no web SDK). Esto no cambia el SDK del proyecto.

### Milo.Api
Reemplazar `Microsoft.AspNetCore.OpenApi` por:
```xml
<PackageReference Include="Serilog.AspNetCore" Version="8.*" />
<PackageReference Include="Serilog.Sinks.File" Version="6.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.*" />
```

JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) y EF Core en runtime están en el shared framework de `Microsoft.NET.Sdk.Web`; no requieren PackageReference explícito.

---

## Diseño de clases

### Domain

```csharp
// Milo.Domain/Common/Interfaces/ISoftDeletable.cs
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}

// Milo.Domain/Common/Interfaces/IAuditable.cs
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
}

// Milo.Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
}

// Milo.Domain/Entities/Enums/UserRole.cs
public enum UserRole
{
    Admin = 1,
    Guest = 2,
    Owner = 3
}

// Milo.Domain/Entities/User.cs
public sealed class User : BaseEntity, IAuditable, ISoftDeletable
{
    private User() { }

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsKycVerified { get; private set; }

    // IAuditable
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    // ISoftDeletable
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public static User Create(string firstName, string lastName, string email,
        string passwordHash, UserRole role) =>
        new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            IsKycVerified = false
        };
}
```

### Application Common

```csharp
// Milo.Application/Common/Models/Result.cs
public sealed class Result<T>
{
    private Result() { }
    public bool IsSuccess { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }

    public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

// Milo.Application/Common/Models/PagedResult.cs
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

// Milo.Application/Common/Interfaces/ICurrentUserProvider.cs
public interface ICurrentUserProvider
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
}
```

```csharp
// Milo.Application/Common/Behaviors/ValidationBehavior.cs
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next(cancellationToken);
    }
}

// Milo.Application/Common/Behaviors/LoggingBehavior.cs
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", name);
        var sw = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        sw.Stop();
        logger.LogInformation("Handled {RequestName} in {ElapsedMs}ms", name, sw.ElapsedMilliseconds);
        return response;
    }
}
```

```csharp
// Milo.Application/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}
```

### Infrastructure

```csharp
// Milo.Infraestructure/Persistence/Interceptors/AuditInterceptor.cs
public sealed class AuditInterceptor(ICurrentUserProvider currentUser) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            ApplyAudit(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext context)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = currentUser.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = currentUser.UserId;
            }
        }
    }
}
```

```csharp
// Milo.Infraestructure/Persistence/MiloDbContext.cs
public sealed class MiloDbContext(DbContextOptions<MiloDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MiloDbContext).Assembly);
        ApplySoftDeleteFilters(modelBuilder);
    }

    private static void ApplySoftDeleteFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

            var method = typeof(MiloDbContext)
                .GetMethod(nameof(GetSoftDeleteFilter), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entityType.ClrType);

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter((LambdaExpression)method.Invoke(null, null)!);
        }
    }

    private static LambdaExpression GetSoftDeleteFilter<TEntity>() where TEntity : ISoftDeletable
    {
        Expression<Func<TEntity, bool>> filter = e => !e.IsDeleted;
        return filter;
    }
}
```

**Nota:** El `SaveChangesAsync` override no es necesario en `MiloDbContext` porque el `AuditInterceptor` (registrado via `AddInterceptors`) intercepta el save antes de que llegue al base. Solo se override si hay lógica adicional de dispatch de domain events (feature futura).

```csharp
// Milo.Infraestructure/Persistence/Configurations/UserConfiguration.cs
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
    }
}
```

```csharp
// Milo.Infraestructure/Persistence/MiloDbContextFactory.cs
public sealed class MiloDbContextFactory : IDesignTimeDbContextFactory<MiloDbContext>
{
    public MiloDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MiloDbContext>()
            .UseNpgsql("Host=localhost;Database=milobnb;Username=postgres;Password=postgres")
            .AddInterceptors(new AuditInterceptor(new NullCurrentUserProvider()))
            .Options;
        return new MiloDbContext(options);
    }
}
```

```csharp
// Milo.Infraestructure/Services/NullCurrentUserProvider.cs
public sealed class NullCurrentUserProvider : ICurrentUserProvider
{
    public Guid? UserId => null;
    public string? Email => null;
    public string? Role => null;
}
```

```csharp
// Milo.Infraestructure/Persistence/Seeders/DbSeeder.cs
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var context = services.GetRequiredService<MiloDbContext>();

        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        var email = configuration["Admin:Email"]!;
        var password = configuration["Admin:Password"]!;

        var hasher = new PasswordHasher<User>();
        var temp = User.Create("Admin", "MiloBnb", email, string.Empty, UserRole.Admin);
        var hash = hasher.HashPassword(temp, password);
        var admin = User.Create("Admin", "MiloBnb", email, hash, UserRole.Admin);

        context.Users.Add(admin);
        await context.SaveChangesAsync();
    }
}
```

```csharp
// Milo.Infraestructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICurrentUserProvider, NullCurrentUserProvider>();
        services.AddScoped<AuditInterceptor>();
        services.AddDbContext<MiloDbContext>((sp, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("Default"))
                .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
        return services;
    }
}
```

### API — Program.cs (estructura completa)

```csharp
using MiloBnb ...

var builder = WebApplication.CreateBuilder(args);

// Serilog — debe registrarse primero para capturar todos los logs
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// Problem Details nativo (RFC 9457)
builder.Services.AddProblemDetails();

// Application + Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Bearer
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!))
        };
    });

builder.Services.AddAuthorization();

// Swagger con soporte JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "MiloBnb API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Ingresa el token JWT"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();

var app = builder.Build();

// Migrations + seed al iniciar
await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MiloDbContext>();
    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
}

// Middleware pipeline (orden estricto)
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

await app.RunAsync();
```

---

## Configuración — appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=milobnb;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "SecretKey": "dev-secret-key-must-be-at-least-32-characters-long",
    "Issuer": "milobnb-api",
    "Audience": "milobnb-clients",
    "ExpirationHours": 24
  },
  "Admin": {
    "Email": "admin@milobnb.com",
    "Password": "Admin1234!"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/milobnb-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

`appsettings.Development.json` solo sobreescribe `Serilog.MinimumLevel.Default: Debug`.

---

## Docker

### Dockerfile (multi-stage)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MiloBnb.slnx .
COPY src/Milo.Domain/Milo.Domain.csproj             src/Milo.Domain/
COPY src/Milo.Application/Milo.Application.csproj   src/Milo.Application/
COPY src/Milo.Infraestructure/Milo.Infraestructure.csproj src/Milo.Infraestructure/
COPY src/Milo.Api/Milo.Api.csproj                   src/Milo.Api/

RUN dotnet restore src/Milo.Api/Milo.Api.csproj

COPY . .

RUN dotnet publish src/Milo.Api/Milo.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN adduser --disabled-password --no-create-home appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Milo.Api.dll"]
```

### docker-compose.yml

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: ${POSTGRES_DB}
      POSTGRES_USER: ${POSTGRES_USER}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__Default: >-
        Host=postgres;Database=${POSTGRES_DB};
        Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}
      Jwt__SecretKey: ${JWT_SECRET_KEY}
      Jwt__Issuer: ${JWT_ISSUER}
      Jwt__Audience: ${JWT_AUDIENCE}
      Admin__Email: ${ADMIN_EMAIL}
      Admin__Password: ${ADMIN_PASSWORD}
    ports:
      - "8080:8080"
    depends_on:
      postgres:
        condition: service_healthy

volumes:
  postgres_data:
```

### .env.example

```
POSTGRES_DB=milobnb
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

JWT_SECRET_KEY=replace-this-with-a-real-secret-32-chars-minimum
JWT_ISSUER=milobnb-api
JWT_AUDIENCE=milobnb-clients

ADMIN_EMAIL=admin@milobnb.com
ADMIN_PASSWORD=Admin1234!
```

---

## Decisiones de diseño

| Decisión | Razón |
|---|---|
| `FrameworkReference` en Infrastructure | `PasswordHasher<T>` pertenece al framework de ASP.NET Core; `FrameworkReference` lo expone sin cambiar el SDK del proyecto |
| `NullCurrentUserProvider` como placeholder | Permite compilar y registrar el grafo DI completo antes de implementar la feature `auth`; sin romper el contrato |
| Seed en runtime, no en migration | La contraseña requiere hasheo dinámico; `HasData` en EF Core exige valores constantes en tiempo de compilación |
| `RequireAuthorization()` global en `MapControllers()` | Opt-out con `[AllowAnonymous]` es más seguro que opt-in; los endpoints públicos se declaran explícitamente |
| Connection string hardcoded en `MiloDbContextFactory` | Solo la usa `dotnet ef` en desarrollo local; nunca llega a producción |
| `AuditInterceptor` registrado como `Scoped` | Depende de `ICurrentUserProvider` que es scoped (contexto HTTP por request); el interceptor vive el mismo tiempo que el DbContext |
| Eliminar `Microsoft.AspNetCore.OpenApi` | Swashbuckle es la decisión arquitectural; los dos packages generarían dos sistemas de OpenAPI en paralelo sin beneficio |
| Orden del pipeline: ExceptionHandler primero | Problem Details captura cualquier excepción no manejada de la capa de auth, validación y handlers |
