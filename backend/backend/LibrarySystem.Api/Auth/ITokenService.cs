using LibrarySystem.Api.Models;

namespace LibrarySystem.Api.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
