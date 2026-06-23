# Estado Actual — MiloBnb

## Feature en progreso
Ninguna.

## Última acción
`reservations` completada. T-01 a T-08 implementadas. `dotnet build` 0 errores, 0 warnings.

Migration: `AddReservations` — tabla Reservations con TotalPrice numeric(18,2), Status varchar(20),
FKs Restrict a Properties y Users, índices en GuestId, PropertyId y compuesto (PropertyId, CheckInDate, CheckOutDate).

TODO Feature 3 cerrado: PropertyRepository.GetAllAsync filtra con subconsulta EF sobre
dbContext.Reservations (Status != Cancelled, solapamiento CheckInDate/CheckOutDate).

Prevención de double-booking: TryCreateSerializableAsync en ReservationRepository abre
transacción IsolationLevel.Serializable, verifica solapamiento, inserta o hace rollback.

## Siguiente paso
Próxima feature a definir (KYC, wishlist, dashboard, etc.)

## Features completadas
- infrastructure-setup (2026-06-23)
- auth (2026-06-23)
- properties (2026-06-23)
- reservations (2026-06-23)
