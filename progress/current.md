# Estado Actual — MiloBnb

## Feature en progreso
Ninguna.

## Última acción
`infrastructure-setup` completada. T-01 a T-08 implementadas. `dotnet build` 0 errores.

Ajuste notable: Swashbuckle fijado a `7.*` (la versión `*` resolvía a 10.x que usa Microsoft.OpenApi v2
con namespaces incompatibles). JwtBearer agregado como PackageReference explícito en Milo.Api.
Microsoft.EntityFrameworkCore.Design agregado también a Milo.Api para que `dotnet ef` funcione.

## Siguiente paso
Feature 1: `auth` (registro, login, JWT real, reemplazar NullCurrentUserProvider)

## Features completadas
- infrastructure-setup (2026-06-23)