using Microsoft.EntityFrameworkCore;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;
using Milo.Infraestructure.Persistence;

namespace Milo.Infraestructure.Persistence.Repositories;

public sealed class KycRepository(MiloDbContext dbContext) : IKycRepository
{
    public async Task<KycVerification?> GetLatestByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.KycVerifications
               .Where(k => k.UserId == userId)
               .OrderByDescending(k => k.CreatedAt)
               .FirstOrDefaultAsync(cancellationToken);

    public void Add(KycVerification verification)
        => dbContext.KycVerifications.Add(verification);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
