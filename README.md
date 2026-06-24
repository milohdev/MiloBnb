# MiloBnb

Plataforma de rentas cortas desarrollada en .NET 10 con Clean Architecture.
Conecta huéspedes con propietarios, automatiza la validación de identidad
por IA y provee herramientas de análisis financiero para propietarios.

## Requisitos previos
- Docker y Docker Compose instalados
- Git

## Levantar el proyecto
1. `git clone <url-del-repositorio>`
2. `cd MiloBnb`
3. `cp .env.example .env`
4. Abrir `.env` y configurar `Anthropic__ApiKey` con una API key válida de Anthropic
   (obtener en https://console.anthropic.com).
   Si no se tiene API key, el sistema usa MockKycService automáticamente
   y el KYC siempre retorna aprobado para fines de demostración.
5. `docker compose up --build`
6. Abrir http://localhost:8080/swagger

Credenciales del Admin por defecto: admin@milobnb.com / Admin1234!

## Arquitectura

Se eligió Clean Architecture en 4 capas porque mantiene el dominio completamente
aislado de frameworks, bases de datos y librerías externas. Esto significa que
la lógica de negocio (reglas de reserva, validación de identidad, cálculo de
métricas) no depende de EF Core, ASP.NET ni ningún proveedor externo —
se puede cambiar PostgreSQL por otro motor, o Anthropic por otro proveedor de IA,
sin tocar una sola línea del dominio.

Las 4 capas y su responsabilidad:
- Domain: entidades, interfaces de repositorio, enums, constantes de negocio.
  No referencia ningún proyecto externo.
- Application: casos de uso como Commands y Queries via CQRS con MediatR.
  Un handler por caso de uso, una sola responsabilidad por clase.
- Infrastructure: implementaciones concretas — EF Core, repositorios,
  servicios externos (KYC, Excel, notificaciones).
- Api: controllers delgados que solo despachan via MediatR, sin lógica de negocio.

CQRS con MediatR se eligió porque separa explícitamente las operaciones de
escritura (Commands) de las de lectura (Queries), hace cada flujo trazable
de forma independiente y permite agregar cross-cutting concerns (validación,
logging) como Pipeline Behaviors sin modificar los handlers.

## Cómo se resolvió cada requerimiento de la prueba

### Requerimientos Generales

**Gestión de Disponibilidad Estricta (prevención de double-booking)**
El handler `CreateReservationHandler` abre una transacción con
`IsolationLevel.Serializable` antes de verificar disponibilidad.
`IReservationRepository.HasOverlappingReservationAsync` consulta si existe
alguna reserva con `Status != Cancelled` para el mismo inmueble donde
`existingCheckIn < checkOut && existingCheckOut > checkIn`.
Si hay solapamiento retorna `Result.Failure`. Si no hay, inserta y confirma
la transacción. El nivel Serializable garantiza que dos requests simultáneos
no puedan pasar la verificación al mismo tiempo — el segundo falla con
error de concurrencia de PostgreSQL.

**Estandarización de Horarios (Check-in 14:00 / Check-out 12:00)**
`BookingConstants.cs` en Domain define `CheckInHour = 14` y `CheckOutHour = 12`
como constantes no configurables. El handler las aplica al crear la reserva:
`CheckInDateTime` y `CheckOutDateTime` se calculan combinando la fecha con la
hora de la constante. Toda reserva confirmada siempre tendrá estos horarios,
sin excepción.

**Sistema Omnicanal de Alertas**
`NotificationService` implementa `INotificationService`. Al dispararse un evento
clave (reserva confirmada, reserva cancelada, KYC aprobado, KYC rechazado),
el handler correspondiente llama a `INotificationService.SendAsync` que:
1. Persiste la notificación en la tabla `Notifications` (in-app).
2. Loguea con Serilog simulando el despacho del email:
   `"Email notification sent to {Email} | Title: {Title} | Type: {Type}"`.

Los endpoints `GET /api/notifications` y `GET /api/notifications/unread` permiten
al usuario consultar sus notificaciones. `PUT /api/notifications/{id}/read`
marca como leída.

**Privacidad y Seguridad de Datos**
`KycVerification` almacena temporalmente el `DocumentImageUrl` solo durante el
procesamiento. Inmediatamente después de que el servicio de IA retorna el
resultado (aprobado o rechazado), el handler limpia el campo
(`DocumentImageUrl = null`) antes del `SaveChangesAsync`. El DTO de respuesta
nunca incluye `DocumentImageUrl`. Los datos extraídos (nombre, apellido,
número de documento) se guardan para referencia pero la imagen original
no persiste en el sistema.

### Rol: Huésped (Guest)

**Búsqueda y Exploración sin registro**
`GET /api/properties` está decorado con `[AllowAnonymous]`. Acepta query params
opcionales: `city`, `checkIn`, `checkOut`, `maxGuests`. Retorna `PagedResult<PropertyDto>`
con paginación. Si se proveen fechas, excluye inmuebles con reservas
confirmadas solapadas. No se requiere token para acceder.

**Gestión de Favoritos (Wishlist)**
`WishlistItem` tiene un constraint único sobre `(GuestId, PropertyId)`.
`POST /api/wishlist/{propertyId}` agrega el inmueble — si ya existe retorna 200
sin error (idempotente). `DELETE /api/wishlist/{propertyId}` lo quita — si no
existe retorna 204 sin error (idempotente). `GET /api/wishlist` lista los
inmuebles activos guardados con todos sus detalles.

**Autenticación Diferida**
El catálogo (`GET /api/properties`) y el detalle (`GET /api/properties/{id}`)
son públicos. La autenticación se exige únicamente al intentar reservar
(`POST /api/reservations`), gestionar favoritos (`POST`/`DELETE /api/wishlist`)
o verificar identidad (`POST /api/kyc/verify`). El registro permite rol
Guest u Owner — el Admin solo existe por seed.

**Validación de Identidad por IA (KYC)**
`POST /api/kyc/verify` recibe una `ImageUrl` (URL pública de la cédula).
`ClaudeKycService` llama a `claude-sonnet-4-6` via API REST de Anthropic,
enviando la imagen como source type `"url"` y solicitando extracción de
`firstName`, `lastName`, `documentNumber` y `birthDate` en JSON. Si la extracción
es exitosa: `KycVerification.Status = Approved` y `User.IsKycVerified = true`.
Si falla: `Status = Rejected` con `RejectionReason` descriptivo.
Sin API key de Anthropic, `MockKycService` simula una aprobación exitosa.
`CreateReservationHandler` verifica `User.IsKycVerified` antes de proceder —
si es `false` retorna 403 con mensaje "Debes completar la verificación de
identidad antes de reservar".

**Gestión de Estancia**
`GET /api/reservations/my` retorna todas las reservas del Guest autenticado
con: `PropertyName`, `CheckInDate`, `CheckOutDate`, `CheckInDateTime` (14:00),
`CheckOutDateTime` (12:00), `TotalNights`, `TotalPrice`, `Status`.
`DELETE /api/reservations/{id}` cancela la reserva (`Status = Cancelled`),
sin eliminación física.

### Rol: Propietario (Owner)

**Gestión de Inventario**
`POST /api/properties` crea un inmueble con: `Name`, `Description`, `Address`, `City`,
`Country`, `PricePerNight`, `MaxGuests`, `Bedrooms`, `Bathrooms`, `AllowSameDayBooking`.
`PUT /api/properties/{id}` edita — solo el Owner dueño del inmueble.
`DELETE /api/properties/{id}` hace soft delete.
`POST /api/properties/{id}/images` agrega una imagen por URL.
`DELETE /api/properties/{id}/images/{imageId}` elimina una imagen.
Todos los endpoints de escritura requieren rol Owner y verifican que
`Property.OwnerId == currentUser.Id`.

**Dashboard de Rendimiento**
`GET /api/dashboard` retorna métricas consolidadas de todos los inmuebles
del Owner: `TotalProperties`, `TotalReservations`, `TotalRevenue`, `OccupancyRate`
(días ocupados / días totales del período * 100), `AveragePricePerNight`,
y `ReservationsByProperty` ordenado por `Revenue` DESC.
`GET /api/dashboard/properties/{id}` detalla un inmueble específico con
`AverageLengthOfStay` y últimas 5 reservas. Ambos endpoints aceptan
query params `dateFrom` y `dateTo` — sin filtro usa los últimos 30 días.

**Extracción de Datos en Excel**
`GET /api/reports/reservations` descarga un `.xlsx` generado con ClosedXML.
Acepta filtros opcionales: `propertyId`, `dateFrom`, `dateTo`. Incluye columnas:
Inmueble, Ciudad, Check-in, Check-out, Noches, Precio Total, Huésped, Email.
Encabezados en verde `#2D6A4F` con texto blanco y negrita.
Filas alternadas blanco/`#F2F2F2`. Fechas en `dd/MM/yyyy`.
Sin resultados: retorna Excel con solo encabezados (no error).

### Feature adicional: Reserva Inmediata (AllowSameDayBooking)

Cada `Property` tiene un flag `AllowSameDayBooking` (bool, default `false`)
configurable por el Owner en el CRUD del inmueble.
`CreateReservationHandler` lo evalúa: si `false`, `CheckInDate` debe ser mínimo
`DateTime.UtcNow.Date.AddDays(1)` — si el huésped intenta reservar para hoy
recibe `Result.Failure` con mensaje claro. Si `true`, `CheckInDate` puede ser
el día actual. Es una decisión de negocio encapsulada en el dominio,
no en el controller.
