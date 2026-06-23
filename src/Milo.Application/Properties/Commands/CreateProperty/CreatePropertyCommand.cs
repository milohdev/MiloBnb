using MediatR;
using Milo.Application.Common.Models;
using Milo.Application.Properties.Queries.GetProperties;

namespace Milo.Application.Properties.Commands.CreateProperty;

public record CreatePropertyCommand(
    string Name,
    string Description,
    string Address,
    string City,
    string Country,
    decimal PricePerNight,
    int MaxGuests,
    int Bedrooms,
    int Bathrooms,
    bool AllowSameDayBooking) : IRequest<Result<PropertyDto>>;
