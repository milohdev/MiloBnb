using Milo.Application.Common.Interfaces;

namespace Milo.Infraestructure.Services;

public sealed class NullCurrentUserProvider : ICurrentUserProvider
{
    public Guid? UserId => null;
    public string? Email => null;
    public string? Role => null;
}
