using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auctions.Commands;

public record CloseAuctionCommand(Guid AuctionId) : IRequest;

public class CloseAuctionCommandHandler : IRequestHandler<CloseAuctionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuctionBroadcaster _broadcaster;
    private readonly IEmailNotificationService _email;

    public CloseAuctionCommandHandler(
        IApplicationDbContext db, IAuctionBroadcaster broadcaster, IEmailNotificationService email)
    {
        _db = db;
        _broadcaster = broadcaster;
        _email = email;
    }

    public async Task Handle(CloseAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _db.GetAuctionForUpdateAsync(request.AuctionId, cancellationToken);
        if (auction is null) return;

        if (auction.Status != AuctionStatus.Active) return;

        var now = DateTime.UtcNow;
        auction.Close(now);

        var item = await _db.Items.FirstAsync(i => i.Id == auction.ItemId, cancellationToken);
        var seller = await _db.Users.FirstOrDefaultAsync(u => u.Id == item.SellerId, cancellationToken);

        if (auction.Status == AuctionStatus.Sold)
        {
            var winningBid = await _db.Bids
                .Where(b => b.AuctionId == auction.Id && b.Status == BidStatus.Winning)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (winningBid is not null)
            {
                var winnerWallet = await _db.Wallets
                    .FirstOrDefaultAsync(w => w.UserId == winningBid.BidderId, cancellationToken);

                if (winnerWallet is not null)
                {
                    winnerWallet.Deduct(winningBid.Amount);

                    var transaction = WalletTransaction.Create(
                        winnerWallet.Id, WalletTransactionType.Deduct, winningBid.Amount,
                        referenceId: auction.Id.ToString());
                    ((Microsoft.EntityFrameworkCore.DbContext)_db).Add(transaction);
                }

                var result = AuctionResult.Create(
                    auction.Id, auction.ItemId, winningBid.Id, winningBid.Amount, AuctionOutcome.Sold);
                ((Microsoft.EntityFrameworkCore.DbContext)_db).Add(result);

                await _db.SaveChangesAsync(cancellationToken);

                // Notifikasi setelah data tersimpan, supaya tidak kirim email
                // kalau ternyata transaksi database gagal di tengah jalan.
                var winner = await _db.Users.FirstOrDefaultAsync(u => u.Id == winningBid.BidderId, cancellationToken);
                if (winner is not null)
                    await _email.SendAuctionWonEmailAsync(winner.Email, winner.FullName, item.Title, winningBid.Amount, cancellationToken);

                if (seller is not null)
                    await _email.SendAuctionEndedSellerEmailAsync(seller.Email, seller.FullName, item.Title, sold: true, winningBid.Amount, cancellationToken);
            }
        }
        else // Unsold
        {
            var lastBid = await _db.Bids
                .Where(b => b.AuctionId == auction.Id && b.Status == BidStatus.Winning)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (lastBid is not null)
            {
                var wallet = await _db.Wallets
                    .FirstOrDefaultAsync(w => w.UserId == lastBid.BidderId, cancellationToken);

                if (wallet is not null)
                {
                    wallet.Release(lastBid.Amount);

                    var transaction = WalletTransaction.Create(
                        wallet.Id, WalletTransactionType.Release, lastBid.Amount,
                        referenceId: auction.Id.ToString());
                    ((Microsoft.EntityFrameworkCore.DbContext)_db).Add(transaction);
                }
            }

            var result = AuctionResult.Create(
                auction.Id, auction.ItemId, winningBidId: null, finalPrice: null, AuctionOutcome.Unsold);
            ((Microsoft.EntityFrameworkCore.DbContext)_db).Add(result);

            await _db.SaveChangesAsync(cancellationToken);

            if (seller is not null)
                await _email.SendAuctionEndedSellerEmailAsync(seller.Email, seller.FullName, item.Title, sold: false, finalPrice: null, cancellationToken);
        }

        await _broadcaster.BroadcastAuctionEndedAsync(auction.Id, cancellationToken);
    }
}