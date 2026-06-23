# Feature: auth — Requerimientos

## Objetivo
Implementar registro, login y perfil de usuario con JWT. Reemplazar el `NullCurrentUserProvider` por una implementación real que lea los claims del contexto HTTP.

## Contexto
- La entidad `User`, `BaseEntity`, `IAuditable`, `ISoftDeletable` ya existen en `Milo.Domain`
- `ICurrentUserProvider` está definido en `Milo.Application.Common.Interfaces` con `UserId?`, `Email?`, `Role?`
- JWT Bearer ya está configurado en `Program.cs` (issuer, audience, signing key)
- `appsettings.json` tiene actualmente `"ExpirationHours": 24` → se cambia a `"ExpirationMinutes": 480`

## Alcance

### RF-01 — Registro de usuario (POST /api/auth/register)

**Endpoint público** (`[AllowAnonymous]`) que crea un nuevo usuario.

**Request body:**
| Campo | Tipo | Reglas |
|---|---|---|
| `firstName` | string | requerido, máx 100 caracteres |
| `lastName` | string | requerido, máx 100 caracteres |
| `email` | string | requerido, formato email, máx 256 caracteres |
| `password` | string | requerido, mínimo 8 caracteres |
| `role` | string | requerido, solo `"Guest"` o `"Owner"` (Admin bloqueado) |

**Respuesta 200 OK:** `AuthResponseDto` (userId, firstName, lastName, email, role, token, expiresAt)

**Errores de negocio:**
- Email ya registrado → 409 Conflict
- Validación de campos → 400 Bad Request (Problem Details via FluentValidation)

### RF-02 — Login (POST /api/auth/login)

**Endpoint público** (`[AllowAnonymous]`) que autentica un usuario existente.

**Request body:**
| Campo | Tipo |
|---|---|
| `email` | string |
| `password` | string |

**Respuesta 200 OK:** `AuthResponseDto` (mismo shape que register)

**Errores de negocio:**
- Email no encontrado o contraseña incorrecta → 401 Unauthorized (mensaje genérico, no distinguir cuál falló)
- Usuario inactivo (`IsActive = false`) → 401 Unauthorized
- Usuario eliminado (soft-deleted) → 401 Unauthorized (excluido por query filter global)

### RF-03 — JWT Token

Claims incluidos en el token:
| Claim | Valor |
|---|---|
| `sub` | `user.Id.ToString()` |
| `email` | `user.Email` |
| `role` | `user.Role.ToString()` (ej: `"Admin"`, `"Guest"`, `"Owner"`) |
| `given_name` | `user.FirstName` |
| `family_name` | `user.LastName` |
| `jti` | `Guid.NewGuid().ToString()` |

Configuración: `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpirationMinutes` (default 480).

El JWT Bearer en `Program.cs` se actualiza con `MapInboundClaims = false` y `RoleClaimType = "role"` para que `[Authorize(Roles = "Admin")]` funcione con claim name simple.

### RF-04 — ICurrentUserProvider (implementación real)

`HttpContextCurrentUserProvider` lee los claims del `HttpContext.User`:
- `UserId` → claim `"sub"` parseado como `Guid`
- `Email` → claim `"email"`
- `Role` → claim `"role"`

Reemplaza `NullCurrentUserProvider` en el registro DI de Infrastructure. El `NullCurrentUserProvider` se conserva solo para `MiloDbContextFactory` (migrations).

### RF-05 — Perfil del usuario (GET /api/auth/me)

**Endpoint autenticado** (usa el `RequireAuthorization()` global).

**Respuesta 200 OK:** `CurrentUserDto` (userId, firstName, lastName, email, role, isKycVerified)

**Errores:**
- Usuario no encontrado en DB (edge case: eliminado después de emitir token) → 404

### RF-06 — Política de autorización (documentar, no implementar endpoints)

- Endpoints públicos: catálogo y búsqueda de inmuebles → `[AllowAnonymous]` (se implementa en Feature 2)
- Endpoints protegidos: reservar, wishlist, publicar inmuebles, dashboard, KYC → `[Authorize]` + role donde aplique
- La política global `RequireAuthorization()` ya está en `Program.cs` desde infrastructure-setup
- Los endpoints de otras features simplemente no agregan `[AllowAnonymous]`; los de register y login sí lo necesitan

## Fuera de alcance
- Refresh tokens
- Confirmación de email
- Recuperación de contraseña
- Registro de Admin via endpoint
- OAuth externo, 2FA
- CRUD de usuarios (administración)
- Endpoints de otras features
