using MediatR;
using Milo.Application.Auth;
using Milo.Application.Common.Models;

namespace Milo.Application.Auth.Commands.Register;

public record RegisterCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string Role) : IRequest<Result<AuthResponseDto>>;
