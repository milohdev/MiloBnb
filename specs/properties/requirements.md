# Feature: properties — Requirements

## Contexto
CRUD completo de inmuebles (Property) para propietarios, más catálogo público para visitantes.
Esta feature introduce las entidades `Property` y `PropertyImage` con su infraestructura completa.
No incluye reservas, wishlist ni reviews (son features separadas).

---

## RF-01 — Crear inmueble (Owner autenticado)

`POST /api/properties`

Solo usuarios con rol `Owner`. OwnerId se extrae del JWT; no va en el cuerpo de la petición.

Campos del cuerpo: `Name`, `Description`, `Address`, `City`, `Country`, `PricePerNight`,
`MaxGuests`, `Bedrooms`, `Bathrooms`, `AllowSameDayBooking` (bool, default `false`).

Validaciones (FluentValidation, ValidationBehavior lanza 400 automáticamente):
- `Name`: requerido, máx 200 caracteres
- `Description`: requerido, máx 2000 caracteres
- `Address`, `City`, `Country`: requeridos, máx 200 caracteres cada uno
- `PricePerNight`: mayor a 0
- `MaxGuests`: entre 1 y 20
- `Bedrooms`: entre 0 y 20
- `Bathrooms`: entre 0 y 20

Respuesta exitosa: `201 Created` con `PropertyDto`.

---

## RF-02 — Editar inmueble (Owner dueño)

`PUT /api/properties/{id}`

Solo `Owner`. El handler verifica que `property.OwnerId == currentUser.UserId`.
Si no coincide: `Result.Failure` con código 403.
Si el inmueble no existe: `Result.Failure` con código 404.

Campos editables: todos los del RF-01 (mismo cuerpo).
No se edita `OwnerId` ni `IsActive` por esta ruta (IsActive se administra por operación administrativa futura).

Respuesta exitosa: `200 Ok` con `PropertyDto` actualizado.

---

## RF-03 — Eliminar inmueble (soft delete, Owner dueño)

`DELETE /api/properties/{id}`

Solo `Owner`. El handler verifica `property.OwnerId == currentUser.UserId` (403 si no coincide).
Soft delete: marca `IsDeleted = true`, `DeletedAt = UtcNow`. El query filter global de EF lo excluye
de todas las consultas futuras.

Respuesta exitosa: `204 No Content`.

---

## RF-04 — Agregar imagen a inmueble (Owner dueño)

`POST /api/properties/{id}/images`

Solo `Owner`. El handler verifica que el inmueble existe y pertenece al usuario (403/404 según corresponda).

Cuerpo: `{ "url": "https://..." }` — solo URL de imagen ya alojada, sin file upload.

Respuesta exitosa: `201 Created` con `{ "imageId": "guid" }`.

---

## RF-05 — Eliminar imagen de inmueble (Owner dueño)

`DELETE /api/properties/{id}/images/{imageId}`

Solo `Owner`. Verifica que el inmueble existe y pertenece al usuario; verifica que la imagen
pertenece a ese inmueble. Delete físico (PropertyImage no tiene soft delete).

Respuesta exitosa: `204 No Content`.

---

## RF-06 — Listar inmuebles activos (público, paginado)

`GET /api/properties`

Sin autenticación ([AllowAnonymous]). Retorna solo propiedades con `IsActive = true`
(el query filter global ya excluye las de `IsDeleted = true`).

Parámetros de query (todos opcionales salvo page/pageSize):
| Parámetro  | Tipo     | Default | Descripción |
|------------|----------|---------|-------------|
| `city`     | string   | —       | Filtro exacto (case-insensitive) |
| `checkIn`  | DateOnly | —       | Sólo válido junto con `checkOut` |
| `checkOut` | DateOnly | —       | Debe ser posterior a `checkIn` |
| `maxGuests`| int      | —       | Propiedades con `MaxGuests >= maxGuests` |
| `page`     | int      | 1       | Número de página |
| `pageSize` | int      | 10      | Resultados por página (máx 50) |

**Filtro de fechas**: excluye inmuebles con reservas solapadas. La tabla `Reservations` no existe
aún — el filtro de fechas se recibe y valida pero se ignora en la query de EF (sin error al usuario).
Se documenta en código con un TODO para reimplementar en Feature 3.

Respuesta: `200 Ok` con `PagedResult<PropertyDto>` (items incluyen lista de URLs de imágenes).

---

## RF-07 — Detalle de inmueble (público)

`GET /api/properties/{id}`

Sin autenticación. Retorna `PropertyDto` completo con lista de imágenes.
Si el inmueble no existe o está soft-deleted: 404.

Respuesta: `200 Ok` con `PropertyDto`.

---

## Exclusiones explícitas
- File upload de imágenes (solo URLs)
- Reservas / disponibilidad real (Feature 3)
- Wishlist (Feature 4)
- Reviews / valoraciones
- Endpoint separado para `AllowSameDayBooking` (se edita vía RF-02)
- Endpoints para Admin
- Tests unitarios o de integración
