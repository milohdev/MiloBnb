namespace Milo.Application.Properties.Queries.GetProperties;

public record PropertyDto(
    Guid Id,
    string Name,
    string Description,
    string Address,
    string City,
    string Country,
    decimal PricePerNight,
    int MaxGuests,
    int Bedrooms,
    int Bathrooms,
    bool AllowSameDayBooking,
    bool IsActive,
    Guid OwnerId,
    IReadOnlyList<string> Images,
    DateTime CreatedAt);
