# Estado Actual — MiloBnb

## Feature en progreso
Ninguna.

## Última acción
`wishlist` completada. T-01 a T-05 implementadas. `dotnet build` 0 errores, 0 warnings.

Migration: `AddWishlist` — tabla WishlistItems con IAuditable fields, UNIQUE index en
(GuestId, PropertyId), índice en GuestId, FKs Cascade a Properties y Users.

WishlistItem: no ISoftDeletable (delete físico). `DeleteAsync` idempotente en el repo.
`GetByGuestIdAsync` usa Include → ThenInclude + filtro IsActive, OrderByDescending CreatedAt.
`AddToWishlistHandler` verifica ExistsAsync antes de Add para respetar UNIQUE constraint.
`GetWishlistHandler` reutiliza PropertyDto de Feature 2.
`WishlistController` con [Authorize(Roles = "Guest")] a nivel de clase. POST → 200, DELETE → 204 siempre.

## Siguiente paso
Próxima feature a definir.

## Features completadas
- infrastructure-setup (2026-06-23)
- auth (2026-06-23)
- properties (2026-06-23)
- reservations (2026-06-23)
- wishlist (2026-06-23)
