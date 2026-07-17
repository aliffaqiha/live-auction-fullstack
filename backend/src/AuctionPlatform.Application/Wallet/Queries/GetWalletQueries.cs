using AuctionPlatform.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Wallets.Queries;

public record WalletTransactionDto(Guid Id, string Type, decimal Amount, string? ReferenceId, DateTime CreatedAt);

public record GetWalletResult(decimal Balance, decimal HeldBalance, decimal AvailableBalance, List<WalletTransactionDto> RecentTransactions);

public record GetWalletQuery(Guid UserId) : IRequest<GetWalletResult>;

public class GetWalletQueryHandler : IRequestHandler<GetWalletQuery, GetWalletResult>
{
    private readonly IApplicationDbContext _db;

    public GetWalletQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<GetWalletResult> Handle(GetWalletQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Wallet tidak ditemukan.");

        var transactions = await _db.WalletTransactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .Select(t => new WalletTransactionDto(t.Id, t.Type.ToString(), t.Amount, t.ReferenceId, t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new GetWalletResult(
            wallet.Balance,
            wallet.HeldBalance,
            wallet.Balance - wallet.HeldBalance,
            transactions
        );
    }
}