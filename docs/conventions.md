# Convenciones de Código — MiloBnb

## Naming
- Clases, métodos, propiedades: PascalCase
- Variables locales, parámetros: camelCase
- Interfaces: prefijo I (IPropertyRepository)
- Commands: verbo + sustantivo + Command (CreatePropertyCommand)
- Queries: Get + sustantivo + Query (GetPropertiesQuery)
- Handlers: mismo nombre + Handler (CreatePropertyHandler)
- DTOs: sustantivo + Dto (PropertyDto)
- Validators: mismo nombre + Validator (CreatePropertyCommandValidator)
- Domain Events: sustantivo + pasado + Event (ReservationCreatedEvent)

## Estructura de carpetas en Application

Application/

├── Common/
│   ├── Behaviors/          # ValidationBehavior, LoggingBehavior
│   ├── Interfaces/         # INotificationService, IKycService, IExcelReportService
│   └── Models/             # Result<T>, PagedResult<T>
├── [Feature]/
│   ├── Commands/
│   │   └── Create[Feature]/
│   │       ├── Create[Feature]Command.cs
│   │       ├── Create[Feature]Handler.cs
│   │       └── Create[Feature]Validator.cs
│   ├── Queries/
│   │   └── Get[Feature]s/
│   │       ├── Get[Feature]sQuery.cs
│   │       ├── Get[Feature]sHandler.cs
│   │       └── [Feature]Dto.cs
│   └── Events/
│       └── [Feature]CreatedEventHandler.cs


## Reglas generales
- Un archivo por clase
- Métodos async con sufijo Async en repos e infra (no en handlers)
- No usar regiones (#region)
- Inyección de dependencias solo por constructor
- Strings mágicos → constantes o enums
- Entidades validan su propio estado (constructor privado + factory methods)
- Soft delete: nunca DELETE físico en entidades críticas
- Logs: siempre ILogger con Serilog, nunca Console.WriteLine
- Errores de negocio: Result<T>. Errores inesperados: Problem Details nativo

## Controllers
- Solo despachan via MediatR, cero lógica de negocio
- Reciben DTOs o records simples, nunca entidades
- Retornan DTOs, nunca entidades de dominio
- Un controller por feature/recurso
- Usan atributos [Authorize(Roles = "...")] donde corresponda
- Endpoints públicos (catálogo, búsqueda) marcados con [AllowAnonymous]

## Validación
- FluentValidation para validar Commands y Queries
- ValidationBehavior en pipeline de MediatR atrapa y lanza ValidationException
- Problem Details nativo convierte ValidationException a 400 con detalles
- Las entidades validan invariantes de negocio internamente (factory methods)

## Autenticación y Autorización
- JWT Bearer tokens
- Roles: Admin, Guest, Owner
- Registro y login devuelven token JWT
- Navegación anónima permitida para catálogo y búsqueda
- Autenticación requerida para: reservar, wishlist, publicar inmuebles, dashboard

## Constantes de dominio
- CHECK_IN_HOUR = 14 (2:00 PM)
- CHECK_OUT_HOUR = 12 (12:00 PM mediodía)
- Se aplican automáticamente, no son configurables