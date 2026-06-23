namespace Milo.Application.Kyc.Queries.GetKycStatus;

public record KycVerificationDto(
    Guid Id,
    Guid UserId,
    string Status,
    string? ExtractedFirstName,
    string? ExtractedLastName,
    string? ExtractedDocumentNumber,
    DateOnly? ExtractedBirthDate,
    string? RejectionReason,
    DateTime CreatedAt);
