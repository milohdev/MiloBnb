# Feature: notifications — Requirements

## Contexto
Notificaciones in-app guardadas en BD. Se disparan sincrónicamente desde handlers existentes
(CreateReservation, CancelReservation, VerifyKyc) sin queue ni background jobs.
El "envío de email" se simula con un log estructurado — no hay integración real con ningún proveedor.

---

## RF-01 — Listar mis notificaciones (paginadas)

`GET /api/notifications?page=1&pageSize=20`

Cualquier usuario autenticado (Guest u Owner).

- Retorna todas las notificaciones del usuario autenticado, ordenadas por `CreatedAt DESC`.
- Paginación con `page` (default 1) y `pageSize` (default 20).
- Respuesta: **200 Ok** con `PagedResult<NotificationDto>`.

---

## RF-02 — Listar notificaciones no leídas

`GET /api/notifications/unread`

Cualquier usuario autenticado.

- Retorna solo las notificaciones con `IsRead = false`, ordenadas por `CreatedAt DESC`.
- Respuesta: **200 Ok** con `IReadOnlyList<NotificationDto>` (sin paginación — son las no leídas).

---

## RF-03 — Marcar notificación como leída

`PUT /api/notifications/{id}/read`

Cualquier usuario autenticado.

- Solo el propietario de la notificación puede marcarla (`notification.UserId == currentUser.UserId`).
  Si no es el dueño: `Result.Failure` → **403 Forbidden**.
- Si la notificación no existe: `Result.Failure` → **404 Not Found**.
- Idempotente: si ya estaba leída, retorna **204 No Content** sin error.
- Respuesta exitosa: **204 No Content**.

---

## NotificationDto

```
Id, Title, Body, Type (string), IsRead, RelatedEntityId (Guid?), CreatedAt
```

---

## Disparos automáticos de notificaciones (handlers existentes)

### CreateReservationHandler — tras confirmar reserva

**Guest** (`reservation.GuestId`):
- Type: `ReservationConfirmed`
- Title: `"Reserva confirmada"`
- Body: `$"Tu reserva en {PropertyName} del {CheckInDate} al {CheckOutDate} fue confirmada."`
- RelatedEntityId: `reservation.Id`

**Owner** (`reservation.Property.OwnerId`):
- Type: `ReservationConfirmed`
- Title: `"Nueva reserva"`
- Body: `$"Tienes una nueva reserva en {PropertyName} del {CheckInDate} al {CheckOutDate}."`
- RelatedEntityId: `reservation.Id`

### CancelReservationHandler — tras cancelar

**Guest** (`reservation.GuestId`):
- Type: `ReservationCancelled`
- Title: `"Reserva cancelada"`
- Body: `$"Tu reserva en {PropertyName} del {CheckInDate} al {CheckOutDate} fue cancelada."`
- RelatedEntityId: `reservation.Id`

La reserva cargada por `GetByIdAsync` ya incluye `Property` (navigation), por lo que
`reservation.Property.Name` está disponible sin query adicional.

### VerifyKycHandler — tras procesar KYC

**Si Approved:**
- Type: `KycApproved`
- Title: `"Identidad verificada"`
- Body: `"Tu identidad fue verificada exitosamente. Ya puedes realizar reservas."`
- RelatedEntityId: `verification.Id`

**Si Rejected:**
- Type: `KycRejected`
- Title: `"Verificación rechazada"`
- Body: `$"No pudimos verificar tu identidad. Motivo: {verification.RejectionReason}"`
- RelatedEntityId: `verification.Id`

---

## Exclusiones
- Tests
- WebSockets / SignalR
- Push notifications móviles
- Integración real con proveedor de email (SendGrid, SES, etc.)
- Plantillas HTML
- Eliminación de notificaciones
- Queue o background jobs
