using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Wallets.Commands;

public record TopUpWalletCommand(Guid UserId, decimal Amount) : IRequest<TopUpWalletResult>;

public record TopUpWalletResult(decimal NewBalance);

public class TopUpWalletCommandHandler : IRequestHandler<TopUpWalletCommand, TopUpWalletResult>
{
    private readonly IApplicationDbContext _db;

    public TopUpWalletCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<TopUpWalletResult> Handle(TopUpWalletCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new InvalidOperationException("Jumlah top up harus lebih dari 0.");

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet tidak ditemukan.");

        wallet.TopUp(request.Amount);

        var transaction = WalletTransaction.Create(
            wallet.Id, WalletTransactionType.TopUp, request.Amount, referenceId: null);

        ((Microsoft.EntityFrameworkCore.DbContext)_db).Add(transaction);

        await _db.SaveChangesAsync(cancellationToken);

        return new TopUpWalletResult(wallet.Balance);
    }
}