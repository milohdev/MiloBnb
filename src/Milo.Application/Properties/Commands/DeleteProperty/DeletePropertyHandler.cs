using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Repositories;

namespace Milo.Application.Properties.Commands.DeleteProperty;

public sealed class DeletePropertyHandler(
    IPropertyRepository repository,
    ICurrentUserProvider currentUser) : IRequestHandler<DeletePropertyCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeletePropertyCommand request, CancellationToken cancellationToken)
    {
        var property = await repository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null)
            return Result<bool>.Failure("Inmueble no encontrado");

        if (property.OwnerId != currentUser.UserId)
            return Result<bool>.Failure("No tienes permiso para eliminar este inmueble");

        property.SoftDelete();
        await repository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
