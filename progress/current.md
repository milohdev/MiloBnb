# Estado Actual — MiloBnb

## Feature en progreso
Ninguna.

## Última acción
`kyc` completada. T-01 a T-07 implementadas. `dotnet build` 0 errores, 0 warnings.

Migration: `AddKycVerification` — tabla KycVerifications con Status varchar(20),
ExtractedBirthDate date, FK Restrict a Users, índice en UserId.

KycVerification: no ISoftDeletable (registros de auditoría inmutables). Métodos Approve()/Reject()
borran DocumentImageUrl antes de persistir. Commit único en VerifyKycHandler: kycRepository.SaveChangesAsync
persiste KycVerification + User.IsKycVerified en un solo round-trip (misma instancia de DbContext).
ClaudeKycService usa AddHttpClient<IKycService>, configura headers en constructor,
parsea respuesta Anthropic buscando campo "error" o campos de extracción.
KycController: [Authorize(Roles = "Guest")] a nivel de clase. POST /verify → 200/409.
GET /status → 200/404.

## Siguiente paso
Próxima feature a definir.

## Features completadas
- infrastructure-setup (2026-06-23)
- auth (2026-06-23)
- properties (2026-06-23)
- reservations (2026-06-23)
- wishlist (2026-06-23)
- kyc (2026-06-23)
