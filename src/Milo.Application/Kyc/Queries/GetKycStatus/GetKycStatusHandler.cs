using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Domain.Entities;
using Milo.Domain.Repositories;

namespace Milo.Application.Kyc.Queries.GetKycStatus;

public sealed class GetKycStatusHandler(
    IKycRepository kycRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<GetKycStatusQuery, Result<KycVerificationDto>>
{
    public async Task<Result<KycVerificationDto>> Handle(
        GetKycStatusQuery request, CancellationToken cancellationToken)
    {
        var verification = await kycRepository.GetLatestByUserIdAsync(
            currentUser.UserId!.Value, cancellationToken);

        if (verification is null)
            return Result<KycVerificationDto>.Failure("No has iniciado el proceso de verificación");

        return Result<KycVerificationDto>.Success(ToDto(verification));
    }

    private static KycVerificationDto ToDto(KycVerification v) =>
        new(v.Id, v.UserId, v.Status.ToString(),
            v.ExtractedFirstName, v.ExtractedLastName, v.ExtractedDocumentNumber,
            v.ExtractedBirthDate, v.RejectionReason, v.CreatedAt);
}
