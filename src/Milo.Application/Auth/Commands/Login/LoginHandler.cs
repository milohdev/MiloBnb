using MediatR;
using Milo.Application.Auth;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Repositories;

namespace Milo.Application.Auth.Commands.Login;

public sealed class LoginHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService) : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        LoginCommand request, CancellationToken cancellationToken)
    {
        // El query filter global excluye soft-deleted → null si está eliminado
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !passwordService.Verify(user.PasswordHash, request.Password))
            return Result<AuthResponseDto>.Failure("Credenciales inválidas");

        if (!user.IsActive)
            return Result<AuthResponseDto>.Failure("La cuenta está desactivada");

        var (token, expiresAt) = jwtTokenService.GenerateToken(user);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.Role.ToString(), token, expiresAt));
    }
}
