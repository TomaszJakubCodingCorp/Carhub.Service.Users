namespace Carhub.Service.Users.Core.Services;

public interface IPasswordManager
{
    void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt);
    bool VerifyPassword(string password, byte[] passwordHash, byte[] passwordSalt);
    void VerifyPasswordRequirements(string password);
}