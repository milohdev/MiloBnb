using Milo.Domain.Common;
using Milo.Domain.Common.Interfaces;
using Milo.Domain.Entities.Enums;

namespace Milo.Domain.Entities;

public sealed class User : BaseEntity, IAuditable, ISoftDeletable
{
    private User() { }

    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsKycVerified { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public void MarkKycVerified() => IsKycVerified = true;

    public static User Create(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        UserRole role) =>
        new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
            IsKycVerified = false
        };
}
