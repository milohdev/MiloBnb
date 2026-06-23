using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Properties.Commands.DeleteProperty;

public record DeletePropertyCommand(Guid PropertyId) : IRequest<Result<bool>>;
