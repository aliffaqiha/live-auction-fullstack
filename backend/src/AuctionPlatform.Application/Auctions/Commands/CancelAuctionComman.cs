using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auctions.Commands;

public record CancelAuctionCommand(Guid AuctionId, Guid SellerId) : IRequest;

/// <summary>
/// Seller membatalkan lelang. Kalau sudah ada bid masuk, saldo semua bidder
/// yang sedang di-hold harus di-release sebelum auction di-cancel.
/// </summary>
public class CancelAuctionCommandHandler : IRequestHandler<CancelAuctionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuctionBroadcaster _broadcaster;

    public CancelAuctionCommandHandler(IApplicationDbContext db, IAuctionBroadcaster broadcaster)
    {
        _db = db;
        _broadcaster = broadcaster;
    }

    public async Task Handle(CancelAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _db.GetAuctionForUpdateAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Lelang tidak ditemukan.");

        // Verifikasi ownership lewat item
        var item = await _db.Items
            .FirstOrDefaultAsync(i => i.Id == auction.ItemId, cancellationToken)
            ?? throw new InvalidOperationException("Item tidak ditemukan.");

        if (item.SellerId != request.SellerId)
            throw new InvalidOperationException("Anda bukan pemilik lelang ini.");

        // Release hold semua bidder yang masih aktif (status Winning)
        var activeBids = await _db.Bids
            .Where(b => b.AuctionId == auction.Id && b.Status == BidStatus.Winning)
            .ToListAsync(cancellationToken);

        var dbContext = (Microsoft.EntityFrameworkCore.DbContext)_db;

        foreach (var bid in activeBids)
        {
            var wallet = await _db.Wallets
                .FirstOrDefaultAsync(w => w.UserId == bid.BidderId, cancellationToken);

            if (wallet is not null)
            {
                wallet.Release(bid.Amount);

                var releaseTransaction = Domain.Entities.WalletTransaction.Create(
                    wallet.Id, WalletTransactionType.Release, bid.Amount,
                    referenceId: auction.Id.ToString());
                dbContext.Add(releaseTransaction);
            }
        }

        auction.Cancel();

        // Buat AuctionResult dengan outcome Cancelled
        var result = Domain.Entities.AuctionResult.Create(
            auction.Id, auction.ItemId,
            winningBidId: null, finalPrice: null,
            AuctionOutcome.Cancelled);
        dbContext.Add(result);

        await _db.SaveChangesAsync(cancellationToken);
        await _broadcaster.BroadcastAuctionEndedAsync(auction.Id, cancellationToken);
    }
}