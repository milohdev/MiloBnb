using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Notifications.Commands.MarkNotificationRead;

public record MarkNotificationReadCommand(Guid NotificationId) : IRequest<Result<bool>>;
