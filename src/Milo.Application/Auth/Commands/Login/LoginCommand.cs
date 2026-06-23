using MediatR;
using Milo.Application.Auth;
using Milo.Application.Common.Models;

namespace Milo.Application.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password) : IRequest<Result<AuthResponseDto>>;
