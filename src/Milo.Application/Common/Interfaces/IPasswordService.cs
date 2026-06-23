namespace Milo.Application.Common.Interfaces;

public interface IPasswordService
{
    string Hash(string plainText);
    bool Verify(string hashedPassword, string plainText);
}
