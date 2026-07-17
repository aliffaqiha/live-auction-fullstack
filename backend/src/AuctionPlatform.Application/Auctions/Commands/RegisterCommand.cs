using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auth.Commands;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string Role
) : IRequest<RegisterResult>;

public record RegisterResult(Guid UserId, string Email, string FullName);

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _hasher;

    public RegisterCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailExists)
            throw new InvalidOperationException("Email sudah terdaftar.");

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
            throw new InvalidOperationException("Role tidak valid. Gunakan: Bidder, Seller, atau Both.");

        var passwordHash = _hasher.Hash(request.Password);
        var user = User.Register(request.Email, passwordHash, request.FullName, role);
        user.MarkAsVerified(); // auto-verify untuk development

        var wallet = Wallet.CreateFor(user.Id);

        // Tambah saldo awal untuk Bidder supaya langsung bisa bid
        if (role is UserRole.Bidder or UserRole.Both)
            wallet.TopUp(1_000_000);

        // EF Core tracking lewat DbContext.Add
        ((Microsoft.EntityFrameworkCore.DbContext)_db).Add(user);
        ((Microsoft.EntityFrameworkCore.DbContext)_db).Add(wallet);

        await _db.SaveChangesAsync(cancellationToken);

        return new RegisterResult(user.Id, user.Email, user.FullName);
    }
}