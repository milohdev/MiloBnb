using MediatR;
using Milo.Application.Auth;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Entities;
using Milo.Domain.Entities.Enums;
using Milo.Domain.Repositories;

namespace Milo.Application.Auth.Commands.Register;

public sealed class RegisterHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService) : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(
        RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email, cancellationToken))
            return Result<AuthResponseDto>.Failure("El email ya está registrado");

        var role = Enum.Parse<UserRole>(request.Role, ignoreCase: true);
        var hash = passwordService.Hash(request.Password);
        var user = User.Create(request.FirstName, request.LastName, request.Email, hash, role);

        userRepository.Add(user);
        await userRepository.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = jwtTokenService.GenerateToken(user);
        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.Role.ToString(), token, expiresAt));
    }
}
