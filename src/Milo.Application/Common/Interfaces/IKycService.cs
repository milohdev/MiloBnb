namespace Milo.Application.Common.Interfaces;

public interface IKycService
{
    Task<KycExtractionResult> ExtractDocumentDataAsync(
        string imageUrl, CancellationToken cancellationToken = default);
}

public record KycExtractionResult(
    string? FirstName,
    string? LastName,
    string? DocumentNumber,
    DateOnly? BirthDate,
    bool IsSuccessful,
    string? FailureReason);
