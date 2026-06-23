using Milo.Domain.Common;
using Milo.Domain.Common.Interfaces;
using Milo.Domain.Constants;
using Milo.Domain.Entities.Enums;

namespace Milo.Domain.Entities;

public sealed class Reservation : BaseEntity, IAuditable, ISoftDeletable
{
    private Reservation() { }

    public Guid PropertyId { get; private set; }
    public Guid GuestId { get; private set; }
    public DateOnly CheckInDate { get; private set; }
    public DateOnly CheckOutDate { get; private set; }
    public DateTime CheckInDateTime { get; private set; }
    public DateTime CheckOutDateTime { get; private set; }
    public int TotalNights { get; private set; }
    public decimal TotalPrice { get; private set; }
    public ReservationStatus Status { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Property Property { get; private set; } = null!;
    public User Guest { get; private set; } = null!;

    public static Reservation Create(
        Guid propertyId, Guid guestId,
        DateOnly checkInDate, DateOnly checkOutDate,
        decimal pricePerNight)
    {
        var nights = checkOutDate.DayNumber - checkInDate.DayNumber;
        return new()
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            GuestId = guestId,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            CheckInDateTime = checkInDate.ToDateTime(
                new TimeOnly(BookingConstants.CheckInHour, 0), DateTimeKind.Utc),
            CheckOutDateTime = checkOutDate.ToDateTime(
                new TimeOnly(BookingConstants.CheckOutHour, 0), DateTimeKind.Utc),
            TotalNights = nights,
            TotalPrice = nights * pricePerNight,
            Status = ReservationStatus.Confirmed
        };
    }

    public void Cancel() => Status = ReservationStatus.Cancelled;
}
