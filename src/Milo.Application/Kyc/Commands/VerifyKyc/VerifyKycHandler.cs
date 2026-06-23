using MediatR;
using Milo.Application.Common.Interfaces;
using Milo.Application.Common.Models;
using Milo.Application.Kyc.Queries.GetKycStatus;
using Milo.Domain.Entities;
using Milo.Domain.Entities.Enums;
using Milo.Domain.Repositories;

namespace Milo.Application.Kyc.Commands.VerifyKyc;

public sealed class VerifyKycHandler(
    IKycRepository kycRepository,
    IKycService kycService,
    IUserRepository userRepository,
    ICurrentUserProvider currentUser) : IRequestHandler<VerifyKycCommand, Result<KycVerificationDto>>
{
    public async Task<Result<KycVerificationDto>> Handle(
        VerifyKycCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId!.Value;

        var existing = await kycRepository.GetLatestByUserIdAsync(userId, cancellationToken);
        if (existing?.Status == KycStatus.Approved)
            return Result<KycVerificationDto>.Failure("Tu identidad ya fue verificada");

        var verification = KycVerification.Create(userId, request.ImageUrl);

        var extraction = await kycService.ExtractDocumentDataAsync(request.ImageUrl, cancellationToken);

        if (extraction.IsSuccessful)
            verification.Approve(
                extraction.FirstName!, extraction.LastName!,
                extraction.DocumentNumber!, extraction.BirthDate);
        else
            verification.Reject(extraction.FailureReason!);

        kycRepository.Add(verification);

        if (verification.Status == KycStatus.Approved)
        {
            var user = await userRepository.GetByIdAsync(userId, cancellationToken);
            user!.MarkKycVerified();
        }

        await kycRepository.SaveChangesAsync(cancellationToken);

        return Result<KycVerificationDto>.Success(ToDto(verification));
    }

    private static KycVerificationDto ToDto(KycVerification v) =>
        new(v.Id, v.UserId, v.Status.ToString(),
            v.ExtractedFirstName, v.ExtractedLastName, v.ExtractedDocumentNumber,
            v.ExtractedBirthDate, v.RejectionReason, v.CreatedAt);
}
