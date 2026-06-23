using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Repositories;

namespace Milo.Application.Auth.Queries.GetCurrentUser;

public sealed class GetCurrentUserHandler(
    IUserRepository userRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    public async Task<Result<CurrentUserDto>> Handle(
        GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result<CurrentUserDto>.Failure("Usuario no autenticado");

        var user = await userRepository.GetByIdAsync(currentUser.UserId.Value, cancellationToken);
        if (user is null)
            return Result<CurrentUserDto>.Failure("Usuario no encontrado");

        return Result<CurrentUserDto>.Success(new CurrentUserDto(
            user.Id, user.FirstName, user.LastName, user.Email,
            user.Role.ToString(), user.IsKycVerified));
    }
}
