using AuctionPlatform.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string Token, string Email, string FullName, string Role);

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public LoginCommandHandler(IApplicationDbContext db, IPasswordHasher hasher, IJwtService jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken)
            ?? throw new InvalidOperationException("Email atau password salah.");

        if (user.IsDeleted)
            throw new InvalidOperationException("Akun ini sudah dihapus.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new InvalidOperationException("Email atau password salah.");

        var token = _jwt.GenerateToken(user);

        return new LoginResult(token, user.Email, user.FullName, user.Role.ToString());
    }
}