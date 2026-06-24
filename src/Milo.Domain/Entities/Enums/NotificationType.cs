namespace Milo.Domain.Entities.Enums;

public enum NotificationType
{
    ReservationConfirmed = 1,
    ReservationCancelled = 2,
    KycApproved = 3,
    KycRejected = 4,
    CheckInReminder = 5,
    CheckOutReminder = 6
}
