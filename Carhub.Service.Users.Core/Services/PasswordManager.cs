using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Carhub.Service.Users.Core.Exceptions;

namespace Carhub.Service.Users.Core.Services;

public sealed partial class PasswordManager : IPasswordManager
{
    public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac
            .ComputeHash(Encoding.UTF8.GetBytes(password));
    }

    public bool VerifyPassword(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512(passwordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(passwordHash);
    }

    public void VerifyPasswordRequirements(string password)
    {
        var missingRequirements = "";

        if (password.Length < 8)
            missingRequirements += "Password must be at least 8 characters long.\n";

        if (!password.Any(char.IsUpper))
            missingRequirements += "Password must contain at least one uppercase letter.\n";

        if (!password.Any(char.IsLower))
            missingRequirements += "Password must contain at least one lowercase letter.\n";

        if (!password.Any(char.IsDigit))
            missingRequirements += "Password must contain at least one digit.\n";

        if (!SpecialCharRegex().IsMatch(password))
            missingRequirements += "Password must contain at least one special character.\n";

        if (string.IsNullOrWhiteSpace(missingRequirements))
            return;

        missingRequirements += "All the requirements must be fulfilled.";
        throw new PasswordRequirementsException(missingRequirements);
    }

    [GeneratedRegex(@"[!@#$%^&*()_\-=+|\\\/<>?\[\]{}'\"":;,.`]")]
    private static partial Regex SpecialCharRegex();
}