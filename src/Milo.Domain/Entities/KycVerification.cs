using Milo.Domain.Common;
using Milo.Domain.Common.Interfaces;
using Milo.Domain.Entities.Enums;

namespace Milo.Domain.Entities;

public sealed class KycVerification : BaseEntity, IAuditable
{
    private KycVerification() { }

    public Guid UserId { get; private set; }
    public string? ExtractedFirstName { get; private set; }
    public string? ExtractedLastName { get; private set; }
    public string? ExtractedDocumentNumber { get; private set; }
    public DateOnly? ExtractedBirthDate { get; private set; }
    public KycStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? DocumentImageUrl { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static KycVerification Create(Guid userId, string imageUrl) =>
        new() { Id = Guid.NewGuid(), UserId = userId, Status = KycStatus.Pending, DocumentImageUrl = imageUrl };

    public void Approve(string firstName, string lastName, string documentNumber, DateOnly? birthDate)
    {
        Status = KycStatus.Approved;
        ExtractedFirstName = firstName;
        ExtractedLastName = lastName;
        ExtractedDocumentNumber = documentNumber;
        ExtractedBirthDate = birthDate;
        DocumentImageUrl = null;
    }

    public void Reject(string reason)
    {
        Status = KycStatus.Rejected;
        RejectionReason = reason;
        DocumentImageUrl = null;
    }
}
