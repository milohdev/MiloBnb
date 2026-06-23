using Microsoft.AspNetCore.Identity;
using Milo.Application.Common.Interfaces;

namespace Milo.Infraestructure.Services;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<string> _hasher = new();

    public string Hash(string plainText) =>
        _hasher.HashPassword(string.Empty, plainText);

    public bool Verify(string hashedPassword, string plainText) =>
        _hasher.VerifyHashedPassword(string.Empty, hashedPassword, plainText)
            != PasswordVerificationResult.Failed;
}
