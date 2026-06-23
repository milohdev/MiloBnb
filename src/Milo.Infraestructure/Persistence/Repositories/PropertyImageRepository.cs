using Microsoft.EntityFrameworkCore;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;
using Milo.Infraestructure.Persistence;

namespace Milo.Infraestructure.Persistence.Repositories;

public sealed class PropertyImageRepository(MiloDbContext dbContext) : IPropertyImageRepository
{
    public async Task<PropertyImage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.PropertyImages.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PropertyImage>> GetByPropertyIdAsync(
        Guid propertyId, CancellationToken cancellationToken = default)
        => await dbContext.PropertyImages
               .Where(i => i.PropertyId == propertyId)
               .ToListAsync(cancellationToken);

    public void Add(PropertyImage image) => dbContext.PropertyImages.Add(image);

    public void Remove(PropertyImage image) => dbContext.PropertyImages.Remove(image);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
