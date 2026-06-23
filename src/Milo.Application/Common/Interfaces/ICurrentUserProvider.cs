namespace Milo.Application.Common.Interfaces;

public interface ICurrentUserProvider
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
}
