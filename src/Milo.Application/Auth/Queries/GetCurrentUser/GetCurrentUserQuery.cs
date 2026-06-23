using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Auth.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<Result<CurrentUserDto>>;
