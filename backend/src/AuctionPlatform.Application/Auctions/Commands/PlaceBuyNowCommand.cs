using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auctions.Commands;

public record PlaceBuyNowCommand(Guid AuctionId, Guid BidderId) : IRequest<PlaceBuyNowResult>;

public record PlaceBuyNowResult(Guid BidId, decimal Amount);

/// <summary>
/// Bidder membeli langsung dengan harga Buy Now. Berbeda dari bid biasa,
/// transaksi ini langsung final (Hold lalu Deduct dalam satu alur), karena
/// begitu Buy Now dipakai, auction langsung ditutup sebagai Sold -- tidak
/// menunggu background lifecycle service memprosesnya lagi.
/// </summary>
public class PlaceBuyNowCommandHandler : IRequestHandler<PlaceBuyNowCommand, PlaceBuyNowResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuctionBroadcaster _broadcaster;

    public PlaceBuyNowCommandHandler(IApplicationDbContext db, IAuctionBroadcaster broadcaster)
    {
        _db = db;
        _broadcaster = broadcaster;
    }

    public async Task<PlaceBuyNowResult> Handle(PlaceBuyNowCommand request, CancellationToken cancellationToken)
    {
        var dbContext = (DbContext)_db;

        var auction = await _db.GetAuctionForUpdateAsync(request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Lelang tidak ditemukan.");

        var item = await _db.Items.FirstAsync(i => i.Id == auction.ItemId, cancellationToken);
        if (item.SellerId == request.BidderId)
            throw new InvalidOperationException("Seller tidak boleh membeli lelang miliknya sendiri.");

        var buyNowPrice = auction.BuyNowPrice
            ?? throw new InvalidOperationException("Lelang ini tidak memiliki opsi Beli Sekarang.");

        var wallet = await _db.Wallets.FirstAsync(w => w.UserId == request.BidderId, cancellationToken);
        if (wallet.AvailableBalance < buyNowPrice)
            throw new InvalidOperationException("Saldo tidak mencukupi untuk Beli Sekarang.");

        // Release hold milik highest bidder sebelumnya (kalau ada), karena
        // buy now langsung mengalahkan siapapun yang sedang unggul.
        var previousHighestBid = await _db.Bids
            .Where(b => b.AuctionId == auction.Id && b.Status == BidStatus.Winning)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousHighestBid is not null)
        {
            var previousWallet = await _db.Wallets.FirstAsync(w => w.UserId == previousHighestBid.BidderId, cancellationToken);
            previousWallet.Release(previousHighestBid.Amount);

            var releaseTransaction = WalletTransaction.Create(
                previousWallet.Id, WalletTransactionType.Release, previousHighestBid.Amount,
                referenceId: auction.Id.ToString());
            dbContext.Add(releaseTransaction);

            previousHighestBid.MarkAsOutbid();
        }

        var now = DateTime.UtcNow;
        auction.BuyNow(request.BidderId, now);

        // Hold lalu Deduct dalam satu alur -- konsisten secara audit trail
        // dengan alur menang lelang biasa (Hold saat bid, Deduct saat settlement),
        // hanya saja di sini keduanya terjadi seketika.
        wallet.Hold(buyNowPrice);
        var holdTransaction = WalletTransaction.Create(
            wallet.Id, WalletTransactionType.Hold, buyNowPrice, referenceId: auction.Id.ToString());
        dbContext.Add(holdTransaction);

        wallet.Deduct(buyNowPrice);
        var deductTransaction = WalletTransaction.Create(
            wallet.Id, WalletTransactionType.Deduct, buyNowPrice, referenceId: auction.Id.ToString());
        dbContext.Add(deductTransaction);

        var bid = Bid.Create(auction.Id, request.BidderId, buyNowPrice);
        bid.MarkAsWinning();
        dbContext.Add(bid);

        var result = AuctionResult.Create(auction.Id, auction.ItemId, bid.Id, buyNowPrice, AuctionOutcome.Sold);
        dbContext.Add(result);

        await _db.SaveChangesAsync(cancellationToken);

        await _broadcaster.BroadcastNewBidAsync(auction.Id, buyNowPrice, request.BidderId, auction.EndTime, cancellationToken);
        await _broadcaster.BroadcastAuctionEndedAsync(auction.Id, cancellationToken);

        return new PlaceBuyNowResult(bid.Id, buyNowPrice);
    }
}