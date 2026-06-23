using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Repositories;

namespace Milo.Application.Properties.Commands.DeletePropertyImage;

public sealed class DeletePropertyImageHandler(
    IPropertyRepository propertyRepository,
    IPropertyImageRepository imageRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<DeletePropertyImageCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeletePropertyImageCommand request, CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null)
            return Result<bool>.Failure("Inmueble no encontrado");

        if (property.OwnerId != currentUser.UserId)
            return Result<bool>.Failure("No tienes permiso para modificar este inmueble");

        var image = await imageRepository.GetByIdAsync(request.ImageId, cancellationToken);
        if (image is null || image.PropertyId != request.PropertyId)
            return Result<bool>.Failure("Imagen no encontrada");

        imageRepository.Remove(image);
        await imageRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
