using Milo.Domain.Entities;

namespace Milo.Domain.Repositories;

public interface IKycRepository
{
    Task<KycVerification?> GetLatestByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(KycVerification verification);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
