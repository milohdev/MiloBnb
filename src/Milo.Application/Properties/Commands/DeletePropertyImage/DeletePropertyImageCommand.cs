using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Properties.Commands.DeletePropertyImage;

public record DeletePropertyImageCommand(Guid PropertyId, Guid ImageId) : IRequest<Result<bool>>;
