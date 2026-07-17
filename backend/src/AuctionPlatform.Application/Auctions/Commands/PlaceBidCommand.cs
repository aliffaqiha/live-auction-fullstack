using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auctions.Commands;

public record PlaceBidCommand(Guid AuctionId, Guid BidderId, decimal Amount) : IRequest<PlaceBidResult>;

public record PlaceBidResult(Guid BidId, decimal Amount, DateTime AuctionEndTime);

public class PlaceBidCommandHandler : IRequestHandler<PlaceBidCommand, PlaceBidResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuctionBroadcaster _broadcaster;
    private readonly IEmailNotificationService _email;


public PlaceBidCommandHandler(
        IApplicationDbContext db, IAuctionBroadcaster broadcaster, IEmailNotificationService email)    {
        _db = db;
        _broadcaster = broadcaster;
        _email = email;
    }

    public async Task<PlaceBidResult> Handle(PlaceBidCommand request, CancellationToken cancellationToken)
    {
        var dbContext = (DbContext)_db;

        var auction = await _db.GetAuctionForUpdateAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Lelang tidak ditemukan.");

        var item = await _db.Items.FirstAsync(i => i.Id == auction.ItemId, cancellationToken);
        if (item.SellerId == request.BidderId)
            throw new InvalidOperationException("Seller tidak boleh bid di lelang miliknya sendiri.");

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == request.BidderId, cancellationToken);

        var now = DateTime.UtcNow;
        var acceptedAmount = auction.AcceptBid(request.BidderId, request.Amount, now);

        var previousHighestBid = await _db.Bids
            .Where(b => b.AuctionId == auction.Id && b.Status == BidStatus.Winning)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousHighestBid is not null && previousHighestBid.BidderId != request.BidderId)
        {
            var previousWallet = await _db.Wallets.FirstAsync(w => w.UserId == previousHighestBid.BidderId, cancellationToken);
            previousWallet.Release(previousHighestBid.Amount);

            var releaseTransaction = WalletTransaction.Create(
                previousWallet.Id, WalletTransactionType.Release, previousHighestBid.Amount,
                referenceId: auction.Id.ToString());
            dbContext.Add(releaseTransaction);

            previousHighestBid.MarkAsOutbid();
            await _broadcaster.BroadcastOutbidAsync(auction.Id, previousHighestBid.BidderId, cancellationToken);
            var outbidUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == previousHighestBid.BidderId, cancellationToken);
    if (outbidUser is not null)
        await _email.SendOutbidEmailAsync(outbidUser.Email, outbidUser.FullName, item.Title, acceptedAmount, cancellationToken);

            
        }

        wallet.Hold(acceptedAmount);

        var holdTransaction = WalletTransaction.Create(
            wallet.Id, WalletTransactionType.Hold, acceptedAmount,
            referenceId: auction.Id.ToString());
        dbContext.Add(holdTransaction);

        var bid = Bid.Create(auction.Id, request.BidderId, acceptedAmount);
        bid.MarkAsWinning();

        // PENTING: Bid adalah entity baru yang belum pernah di-track EF Core.
        // Tanpa baris ini, bid tidak akan pernah tersimpan ke database.
        dbContext.Add(bid);

        await _db.SaveChangesAsync(cancellationToken);

        await _broadcaster.BroadcastNewBidAsync(auction.Id, acceptedAmount, request.BidderId, auction.EndTime, cancellationToken);

        return new PlaceBidResult(bid.Id, acceptedAmount, auction.EndTime);
    }
}