# Arquitectura — MiloBnb

## Dominio
Plataforma de rentas cortas. Huéspedes buscan y reservan inmuebles publicados por propietarios.
El sistema gestiona disponibilidad estricta (prevención de double-booking), validación de identidad
con IA (KYC), wishlist, notificaciones, dashboard de métricas y exportación de reportes Excel.

## Stack tecnológico
- .NET 10
- PostgreSQL 16
- EF Core (ORM)
- MediatR (CQRS)
- FluentValidation
- Serilog (structured logging)
- Docker + Docker Compose (obligatorio)
- Swashbuckle (Swagger con JWT)
- ClosedXML (exportación Excel)

## Arquitectura — Clean Architecture, 4 capas

### Domain (MiloBnb.Domain)
No depende de nada. Es el centro.
- Entidades con constructor privado + factory methods
- Value Objects
- Enums
- Interfaces de repositorio
- Domain Events
- Interfaces: ISoftDeletable, IAuditable
- Clase base: BaseEntity (Guid Id)

### Application (MiloBnb.Application)
Depende solo de Domain.
- Commands y Queries (CQRS via MediatR)
- Un handler por command/query
- DTOs de entrada y salida
- Interfaces de servicios externos (INotificationService, IKycService, IExcelReportService)
- Pipeline Behaviors: ValidationBehavior, LoggingBehavior
- Common: Result<T>, PagedResult<T>

### Infrastructure (MiloBnb.Infrastructure)
Depende de Application.
- DbContext (EF Core) con override de SaveChangesAsync
- Repositorios concretos
- Servicios externos (KYC con Claude Vision API, notificaciones, Excel)
- Configurations de EF Core (Fluent API)
- Migrations
- Query filter global para ISoftDeletable
- Interceptor de auditoría para IAuditable

### Api (MiloBnb.Api)
Depende de Application + Infrastructure.
- Controllers (solo despachan via MediatR, cero lógica)
- Program.cs (DI, middleware, auth)
- Configuración: JWT, Swagger, Serilog

### Regla de dependencia
Domain ← Application ← Infrastructure ← Api
Domain no conoce a nadie. Las capas externas dependen de las internas, nunca al revés.

## Patrones de diseño
- CQRS con MediatR: Commands (escritura) separados de Queries (lectura)
- Repository Pattern: interfaces en Domain, implementaciones en Infrastructure
- Result Pattern: errores de negocio como valores, no excepciones
- Domain Events: entidades emiten eventos, handlers separados los procesan
- Pipeline Behaviors: validación y logging como cross-cutting concerns
- Soft Delete: ISoftDeletable + query filter global en EF Core

## Pipeline del request

Request HTTP

→ Serilog (loguea request)
→ Problem Details nativo (atrapa excepciones → RFC 9457)
→ Authentication (JWT Bearer)
→ Authorization (roles)
→ Controller → MediatR → ValidationBehavior → LoggingBehavior → Handler


## Decisiones técnicas fijas
- Problem Details nativo (AddProblemDetails + UseExceptionHandler), NO middleware custom de excepciones
- ValidationBehavior lanza ValidationException para input inválido (la atrapa Problem Details)
- Result<T> solo para errores de negocio
- PasswordHasher<T> nativo de ASP.NET Core
- Serilog console + file sink
- NO Redis, NO idempotency middleware, NO correlation ID middleware, NO rate limiting
- NO tests en flujo principal (si sobra tiempo, 1-2 unit tests al final)

## Roles del sistema
- Admin: gestión general, seed por defecto
- Guest: huésped/arrendatario — busca, reserva, wishlist, KYC
- Owner: propietario/anfitrión — publica inmuebles, dashboard, reportes

## Entidades principales (modelo conceptual)
- User (base, todos los roles)
- Property (inmueble publicado por Owner)
- PropertyImage (fotos del inmueble)
- Reservation (reserva de Guest en Property)
- WishlistItem (favorito de Guest)
- KycVerification (resultado de validación de identidad)
- Notification (alertas in-app)

## Feature extra: Reserva inmediata
Cada Property tiene un flag AllowSameDayBooking (bool, default false).
- Si true: huéspedes pueden reservar para el mismo día.
- Si false: solo pueden reservar desde el día siguiente en adelante.
  Se configura en el CRUD del inmueble por el Owner.

## Prevención de double-booking
Se implementa con un UNIQUE constraint en PostgreSQL sobre (PropertyId, Date) en una tabla
PropertyDateBlock o similar, combinado con transacción serializable o SELECT FOR UPDATE
al momento de crear la reserva. El handler de CreateReservation debe validar disponibilidad
dentro de una transacción para evitar race conditions.

## Check-in / Check-out estándar
- Check-in: 14:00 (2:00 PM)
- Check-out: 12:00 (mediodía)
  Estos horarios se aplican automáticamente a toda reserva confirmada. Son constantes del dominio,
  no configurables por el usuario.