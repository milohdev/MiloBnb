using MediatR;
using Milo.Application.Common.Models;
using Milo.Application.Kyc.Queries.GetKycStatus;

namespace Milo.Application.Kyc.Commands.VerifyKyc;

public record VerifyKycCommand(string ImageUrl) : IRequest<Result<KycVerificationDto>>;
