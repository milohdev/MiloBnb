using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Properties.Commands.AddPropertyImage;

public sealed class AddPropertyImageHandler(
    IPropertyRepository propertyRepository,
    IPropertyImageRepository imageRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<AddPropertyImageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        AddPropertyImageCommand request, CancellationToken cancellationToken)
    {
        var property = await propertyRepository.GetByIdAsync(request.PropertyId, cancellationToken);
        if (property is null)
            return Result<Guid>.Failure("Inmueble no encontrado");

        if (property.OwnerId != currentUser.UserId)
            return Result<Guid>.Failure("No tienes permiso para modificar este inmueble");

        var image = PropertyImage.Create(request.PropertyId, request.Url);
        imageRepository.Add(image);
        await imageRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(image.Id);
    }
}
