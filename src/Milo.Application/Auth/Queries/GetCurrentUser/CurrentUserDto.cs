namespace Milo.Application.Auth.Queries.GetCurrentUser;

public record CurrentUserDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsKycVerified);
