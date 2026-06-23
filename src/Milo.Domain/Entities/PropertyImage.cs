using Milo.Domain.Common;

namespace Milo.Domain.Entities;

public sealed class PropertyImage : BaseEntity
{
    private PropertyImage() { }

    public Guid PropertyId { get; private set; }
    public string Url { get; private set; } = default!;

    public static PropertyImage Create(Guid propertyId, string url) =>
        new() { Id = Guid.NewGuid(), PropertyId = propertyId, Url = url };
}
