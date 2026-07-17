using AuctionPlatform.Domain.Entities;

namespace AuctionPlatform.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}