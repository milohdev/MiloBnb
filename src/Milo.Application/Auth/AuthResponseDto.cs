namespace Milo.Application.Auth;

public record AuthResponseDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string Token,
    DateTime ExpiresAt);
