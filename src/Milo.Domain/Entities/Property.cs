using Milo.Domain.Common;
using Milo.Domain.Common.Interfaces;

namespace Milo.Domain.Entities;

public sealed class Property : BaseEntity, IAuditable, ISoftDeletable
{
    private Property() { }

    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public decimal PricePerNight { get; private set; }
    public int MaxGuests { get; private set; }
    public int Bedrooms { get; private set; }
    public int Bathrooms { get; private set; }
    public bool AllowSameDayBooking { get; private set; }
    public bool IsActive { get; private set; }
    public Guid OwnerId { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PropertyImage> Images { get; private set; } = [];

    public static Property Create(
        string name, string description, string address, string city, string country,
        decimal pricePerNight, int maxGuests, int bedrooms, int bathrooms,
        bool allowSameDayBooking, Guid ownerId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Address = address,
            City = city,
            Country = country,
            PricePerNight = pricePerNight,
            MaxGuests = maxGuests,
            Bedrooms = bedrooms,
            Bathrooms = bathrooms,
            AllowSameDayBooking = allowSameDayBooking,
            IsActive = true,
            OwnerId = ownerId
        };

    public void Update(
        string name, string description, string address, string city, string country,
        decimal pricePerNight, int maxGuests, int bedrooms, int bathrooms,
        bool allowSameDayBooking)
    {
        Name = name;
        Description = description;
        Address = address;
        City = city;
        Country = country;
        PricePerNight = pricePerNight;
        MaxGuests = maxGuests;
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        AllowSameDayBooking = allowSameDayBooking;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
