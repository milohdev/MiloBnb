# Feature: kyc — Requirements

## Contexto
Validación de identidad de Guests usando IA (Anthropic Claude). El Guest sube la URL pública
de una foto de su cédula; el sistema extrae los datos con la API de Anthropic y registra el
resultado. Si la extracción es exitosa, el Guest queda habilitado para reservar (`IsKycVerified = true`).

El chequeo `IsKycVerified` en `CreateReservationHandler` ya existe — esta feature lo activa.

---

## RF-01 — Iniciar verificación KYC

`POST /api/kyc/verify`  Solo rol `Guest`. GuestId del JWT.

**Body:** `{ "imageUrl": "https://..." }` — URL pública de la foto del documento de identidad.

Reglas:
- Si el Guest ya tiene una verificación con `Status = Approved`: `Result.Failure` → **409 Conflict**
  ("Tu identidad ya fue verificada").
- Se acepta un nuevo intento si la verificación anterior fue `Rejected` (se crea un nuevo registro).
- El sistema llama a `IKycService.ExtractDocumentDataAsync(imageUrl)`:
  - **Extracción exitosa:** `KycVerification.Status = Approved`, campos extraídos poblados,
    `User.IsKycVerified = true`.
  - **Extracción fallida:** `KycVerification.Status = Rejected`, `RejectionReason` con la razón.
- En ambos casos: `DocumentImageUrl` se borra (`null`) del registro antes de persistir.
- **Siempre** se retorna `KycVerificationDto` con el resultado (Approved o Rejected).
- Respuesta exitosa: **200 Ok** con `KycVerificationDto`.

---

## RF-02 — Consultar estado de mi verificación

`GET /api/kyc/status`  Solo rol `Guest`.

- Retorna la verificación más reciente del Guest autenticado.
- Si no ha iniciado ninguna: → **404 Not Found**.
- Respuesta: **200 Ok** con `KycVerificationDto`.

---

## KycVerificationDto

```
Id, UserId, Status (string), ExtractedFirstName, ExtractedLastName,
ExtractedDocumentNumber, ExtractedBirthDate (DateOnly?), RejectionReason, CreatedAt
```

`DocumentImageUrl` **nunca** se incluye en el DTO.

---

## Modelo de reintentos

Un Guest `Rejected` puede llamar a `POST /api/kyc/verify` de nuevo. Se crea un nuevo
registro `KycVerification`; el anterior permanece en BD para auditoría. El sistema
no limita el número de intentos (fuera de alcance).

---

## Exclusiones
- Tests
- Almacenamiento propio de imágenes (el Guest usa cualquier hosting público)
- Queue o procesamiento en background (es síncrono)
- Validación manual por Admin
- Encriptación de datos en BD
- Reintentos automáticos ante fallo de la API de Anthropic
- Límite de intentos
