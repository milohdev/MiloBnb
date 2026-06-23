# Feature: infrastructure-setup — Tasks

> Implementar en este orden exacto. Ejecutar `dotnet build` al terminar cada tarea antes de continuar con la siguiente.
> Las clases y firmas de referencia están en `design.md`.

---

## T-01 — Domain base

**Acciones:**
1. Eliminar `src/Milo.Domain/Class1.cs`
2. Crear `src/Milo.Domain/Common/Interfaces/ISoftDeletable.cs`
3. Crear `src/Milo.Domain/Common/Interfaces/IAuditable.cs`
4. Crear `src/Milo.Domain/Common/BaseEntity.cs`
5. Crear `src/Milo.Domain/Entities/Enums/UserRole.cs`
6. Crear `src/Milo.Domain/Entities/User.cs`

**Checkpoint:** `dotnet build src/Milo.Domain/Milo.Domain.csproj` sin errores.

---

## T-02 — Application: paquetes + modelos comunes

**Acciones:**
1. Eliminar `src/Milo.Application/Class1.cs`
2. Agregar paquetes NuGet a `Milo.Application`:
   ```
   dotnet add src/Milo.Application package MediatR
   dotnet add src/Milo.Application package FluentValidation
   dotnet add src/Milo.Application package FluentValidation.DependencyInjectionExtensions
   ```
3. Crear `src/Milo.Application/Common/Models/Result.cs`
4. Crear `src/Milo.Application/Common/Models/PagedResult.cs`
5. Crear `src/Milo.Application/Common/Interfaces/ICurrentUserProvider.cs`

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj` sin errores.

---

## T-03 — Application: behaviors + DI extension

**Acciones:**
1. Crear `src/Milo.Application/Common/Behaviors/ValidationBehavior.cs`
2. Crear `src/Milo.Application/Common/Behaviors/LoggingBehavior.cs`
3. Crear `src/Milo.Application/DependencyInjection.cs`

`DependencyInjection.cs` registra: `AddMediatR`, `AddValidatorsFromAssembly`, los dos `IPipelineBehavior<,>`.

**Checkpoint:** `dotnet build src/Milo.Application/Milo.Application.csproj` sin errores.

---

## T-04 — Infrastructure: paquetes + DbContext + interceptor + configuraciones

**Acciones:**
1. Eliminar `src/Milo.Infraestructure/Class1.cs`
2. Editar `src/Milo.Infraestructure/Milo.Infraestructure.csproj`:
   - Agregar `<FrameworkReference Include="Microsoft.AspNetCore.App" />`
   - Agregar paquetes:
     ```
     dotnet add src/Milo.Infraestructure package Npgsql.EntityFrameworkCore.PostgreSQL
     dotnet add src/Milo.Infraestructure package Microsoft.EntityFrameworkCore.Design
     ```
3. Crear `src/Milo.Infraestructure/Persistence/Interceptors/AuditInterceptor.cs`
4. Crear `src/Milo.Infraestructure/Persistence/Configurations/UserConfiguration.cs`
5. Crear `src/Milo.Infraestructure/Persistence/MiloDbContext.cs`
6. Crear `src/Milo.Infraestructure/Persistence/MiloDbContextFactory.cs`

`MiloDbContextFactory` necesita `NullCurrentUserProvider` (se crea en T-05); para compilar en T-04 se puede referenciar con un `new NullCurrentUserProvider()` dejando la clase vacía como stub, o crear ambos archivos en T-04.

**Alternativa limpia:** crear también `NullCurrentUserProvider` en este paso (aunque no forme parte del scope de T-04) para que la factory compile sin stub.

**Checkpoint:** `dotnet build src/Milo.Infraestructure/Milo.Infraestructure.csproj` sin errores.

---

## T-05 — Infrastructure: servicios + DI extension + DbSeeder

**Acciones:**
1. Crear `src/Milo.Infraestructure/Services/NullCurrentUserProvider.cs`
   - Implementa `ICurrentUserProvider` devolviendo `null` en todo
2. Crear `src/Milo.Infraestructure/Persistence/Seeders/DbSeeder.cs`
   - Clase estática con método `SeedAsync(IServiceProvider, IConfiguration)`
   - Verifica si ya existe un Admin antes de insertar
   - Hashea con `PasswordHasher<User>`
3. Crear `src/Milo.Infraestructure/DependencyInjection.cs`
   - `AddInfrastructure(IServiceCollection, IConfiguration)` registra: `ICurrentUserProvider` → `NullCurrentUserProvider`, `AuditInterceptor` (Scoped), `MiloDbContext` con `UseNpgsql` + `AddInterceptors`

**Checkpoint:** `dotnet build src/Milo.Infraestructure/Milo.Infraestructure.csproj` sin errores.

---

## T-06 — Migration inicial

**Prerequisito:** `dotnet-ef` instalado globalmente.
```
dotnet tool install --global dotnet-ef   # si no está instalado
```

**Acciones:**
1. Generar la migration:
   ```
   dotnet ef migrations add InitialCreate \
     --project src/Milo.Infraestructure \
     --startup-project src/Milo.Api \
     --output-dir Persistence/Migrations
   ```
2. Verificar que se crearon los archivos en `src/Milo.Infraestructure/Persistence/Migrations/`

La migration usa `MiloDbContextFactory` para construir el contexto sin levantar la app. PostgreSQL no necesita estar corriendo para generar la migration.

**Checkpoint:** Los archivos de migration existen y `dotnet build` sigue sin errores.

---

## T-07 — API: paquetes + Program.cs + appsettings

**Acciones:**
1. Editar `src/Milo.Api/Milo.Api.csproj`:
   - Eliminar `<PackageReference Include="Microsoft.AspNetCore.OpenApi" ... />`
   - Agregar:
     ```
     dotnet add src/Milo.Api package Serilog.AspNetCore
     dotnet add src/Milo.Api package Serilog.Sinks.File
     dotnet add src/Milo.Api package Swashbuckle.AspNetCore
     ```
2. Eliminar `src/Milo.Api/Milo.Api.http`
3. Reescribir `src/Milo.Api/Program.cs` completo según `design.md`
   - Serilog → `AddProblemDetails` → `AddApplication` → `AddInfrastructure` → JWT → `AddAuthorization` → Swagger → `AddControllers`
   - Startup: `MigrateAsync` + `DbSeeder.SeedAsync`
   - Pipeline: `UseExceptionHandler` → Swagger (solo Dev) → `UseAuthentication` → `UseAuthorization` → `MapControllers().RequireAuthorization()`
4. Reemplazar el contenido de `src/Milo.Api/appsettings.json` con la estructura de `design.md` (secciones `ConnectionStrings`, `Jwt`, `Admin`, `Serilog`)
5. Actualizar `src/Milo.Api/appsettings.Development.json`:
   ```json
   {
     "Serilog": {
       "MinimumLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

**Checkpoint:** `dotnet build` desde la raíz sin errores. Al ejecutar `dotnet run --project src/Milo.Api` con PostgreSQL corriendo en localhost, Swagger es accesible en `http://localhost:5000/swagger` (o el puerto configurado).

---

## T-08 — Docker

**Archivos a crear en la raíz del repositorio:**

1. `Dockerfile` — multi-stage según `design.md`
2. `docker-compose.yml` — servicios `postgres` y `api` según `design.md`
3. `.env.example` — todas las variables con valores de ejemplo
4. `.dockerignore`:
   ```
   **/.git
   **/.vs
   **/.idea
   **/bin
   **/obj
   **/logs
   **/*.user
   .env
   ```

**Checkpoint:** `docker compose up --build` levanta ambos servicios sin errores. Swagger accesible en `http://localhost:8080/swagger`. El usuario `admin@milobnb.com` existe en la tabla `users` de PostgreSQL.

---

## Verificación final

Al terminar todas las tasks:

- [ ] `dotnet build` en la raíz: sin errores ni warnings relevantes
- [ ] `docker compose up --build`: ambos servicios healthy
- [ ] `GET http://localhost:8080/swagger`: Swagger UI carga y muestra botón "Authorize"
- [ ] La tabla `users` en PostgreSQL contiene el usuario Admin con contraseña hasheada
- [ ] Los archivos de migration existen en `src/Milo.Infraestructure/Persistence/Migrations/`
- [ ] No existen `Class1.cs` ni `Milo.Api.http` en el proyecto
- [ ] Actualizar `progress/current.md` con feature completada
