using AuthService.Domain;

namespace AuthService.Application.Services;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    (string token, DateTime expiry) GenerateRefreshToken();
    Guid? ValidateToken(string token);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
