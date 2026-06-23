# Feature: wishlist — Requirements

## Contexto
Lista de favoritos para Guests. Permite guardar inmuebles de interés para consultarlos después.
Operaciones idempotentes: agregar un favorito ya existente no es error; eliminar uno que no existe tampoco.
Reutiliza `PropertyDto` de Feature 2 — no se crea DTO nuevo.

---

## RF-01 — Agregar inmueble a favoritos (Guest autenticado)

`POST /api/wishlist/{propertyId}`

Solo rol `Guest`. GuestId tomado del JWT.

- Si el inmueble no existe o no está activo (`IsActive = false` o soft-deleted):
  `Result.Failure` → 404.
- Si ya está en la wishlist del usuario: **idempotente** → `200 Ok` sin error.
- Si no está: crear `WishlistItem`, persistir → `200 Ok`.

Respuesta exitosa: `200 Ok` (sin cuerpo).

---

## RF-02 — Quitar inmueble de favoritos (Guest autenticado)

`DELETE /api/wishlist/{propertyId}`

Solo rol `Guest`.

- Si no existe en la wishlist: **idempotente** → `204 No Content` sin error.
- Si existe: eliminar físicamente (no soft-delete) → `204 No Content`.

Respuesta: `204 No Content` en todos los casos (incluso si no existía).

---

## RF-03 — Listar mis favoritos (Guest autenticado)

`GET /api/wishlist`

Solo rol `Guest`. Retorna los inmuebles favoritos del Guest autenticado.

- Solo incluye inmuebles con `IsActive = true` y no soft-deleted.
  (El query filter global de EF ya excluye soft-deleted al hacer Include de Property.)
- Ordenados por fecha de creación del WishlistItem DESC.

Respuesta: `200 Ok` con `IReadOnlyList<PropertyDto>` (mismo DTO de Feature 2,
incluyendo lista de URLs de imágenes).

---

## Constraint de unicidad

La tabla `WishlistItems` tiene un UNIQUE constraint en `(GuestId, PropertyId)` a nivel de base
de datos. EF lanza excepción si se intenta insertar duplicado sin verificar previamente — la
verificación con `ExistsAsync` en el handler previene esto.

---

## Exclusiones explícitas
- Wishlist para usuarios anónimos (autenticación diferida es responsabilidad del frontend)
- Compartir wishlist
- Notificaciones al guardar favorito
- Comparador de inmuebles
- Tests
