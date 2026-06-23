using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Properties.Commands.AddPropertyImage;

public record AddPropertyImageCommand(Guid PropertyId, string Url) : IRequest<Result<Guid>>;
