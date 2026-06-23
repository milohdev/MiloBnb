using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Milo.Application.Common.Interfaces;

namespace Milo.Infraestructure.Services;

public sealed class HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserProvider
{
    public Guid? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        httpContextAccessor.HttpContext?.User.FindFirstValue("email");

    public string? Role =>
        httpContextAccessor.HttpContext?.User.FindFirstValue("role");
}
