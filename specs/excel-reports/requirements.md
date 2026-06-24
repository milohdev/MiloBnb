# Feature: excel-reports — Requirements

## Contexto
Generación de reportes Excel (.xlsx) sobre datos existentes. Sin entidades nuevas, sin migration.
Solo reservas con `Status = Confirmed` se incluyen en el reporte.
Solo el Owner puede generar su propio reporte.

---

## RF-01 — Descargar reporte de reservaciones

`GET /api/reports/reservations?propertyId=&dateFrom=&dateTo=`  Solo rol `Owner`.

**Query params (todos opcionales):**
- `propertyId` (Guid) — filtrar por un inmueble específico
- `dateFrom` (DateOnly, yyyy-MM-dd) — desde esta fecha de check-in
- `dateTo` (DateOnly, yyyy-MM-dd) — hasta esta fecha de check-in

**Reglas:**
- Si se provee `propertyId`: verificar que el inmueble pertenece al Owner autenticado.
  Si no pertenece o no existe: → **404 Not Found**.
- Si `dateFrom` y `dateTo` son null: incluir todas las reservas Confirmed del Owner (sin filtro de fecha).
- Si solo `dateFrom`: desde esa fecha hasta hoy.
- Si solo `dateTo`: desde inicio de datos hasta esa fecha.
- Si ambos: rango exacto.
- Si no hay reservas que cumplan los filtros: retornar Excel con solo la fila de encabezados
  (no es un error — retorna 200 con archivo vacío pero bien formado).

**Respuesta:**
- **200 Ok** con `Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- `Content-Disposition: attachment; filename="reporte-reservaciones-{yyyyMMdd}.xlsx"`
- Body: `byte[]` del archivo .xlsx

---

## Estructura del Excel

**Hoja:** `"Reservaciones"`

**Encabezados (fila 1):**

| Col | Encabezado | Fuente |
|---|---|---|
| A | Inmueble | `PropertyName` |
| B | Ciudad | `PropertyCity` |
| C | Check-in | `CheckInDate` (dd/MM/yyyy) |
| D | Check-out | `CheckOutDate` (dd/MM/yyyy) |
| E | Noches | `TotalNights` |
| F | Precio Total | `TotalPrice` (2 decimales) |
| G | Huésped | `$"{GuestFirstName} {GuestLastName}"` |
| H | Email | `GuestEmail` |

**Estilo de encabezados:**
- Fondo: `#2D6A4F` (verde oscuro)
- Texto: blanco, negrita

**Filas de datos:**
- Alternadas: blanco (impares) y `#F2F2F2` gris claro (pares).
- Fechas: formato `dd/MM/yyyy`.
- `TotalPrice`: formato numérico `#,##0.00`.

**Columnas:** ancho automático (`AdjustToContents`).

---

## Exclusiones
- Tests
- Reportes para Admin
- Exportación PDF
- Reportes programados o envío por email
- Paginación del reporte
