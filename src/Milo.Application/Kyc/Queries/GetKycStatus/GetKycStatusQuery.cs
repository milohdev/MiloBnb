using MediatR;
using Milo.Application.Common.Models;

namespace Milo.Application.Kyc.Queries.GetKycStatus;

public record GetKycStatusQuery : IRequest<Result<KycVerificationDto>>;
