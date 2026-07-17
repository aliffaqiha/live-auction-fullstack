using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auctions.Commands;

public record CreateAuctionCommand(
    Guid ItemId,
    Guid SellerId,
    decimal StartingPrice,
    decimal? ReservePrice,
    decimal BidIncrement,
    decimal? BuyNowPrice,
    DateTime StartTime,
    DateTime EndTime
) : IRequest<Guid>;

public class CreateAuctionCommandHandler : IRequestHandler<CreateAuctionCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateAuctionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateAuctionCommand request, CancellationToken cancellationToken)
    {
        var item = await _db.Items
            .FirstOrDefaultAsync(i => i.Id == request.ItemId && !i.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Item tidak ditemukan.");

        if (item.SellerId != request.SellerId)
            throw new InvalidOperationException("Kamu bukan pemilik item ini.");

        // Cek apakah item sudah punya lelang aktif
        var hasActiveAuction = await _db.Auctions.AnyAsync(
            a => a.ItemId == request.ItemId &&
            (a.Status == Domain.Enums.AuctionStatus.Active || a.Status == Domain.Enums.AuctionStatus.Scheduled),
            cancellationToken);

        if (hasActiveAuction)
            throw new InvalidOperationException("Item ini sudah memiliki lelang yang sedang berjalan.");

        // Hitung relist count dari auction sebelumnya
        var relistCount = await _db.Auctions.CountAsync(a => a.ItemId == request.ItemId, cancellationToken);

        var auction = Auction.Create(
            request.ItemId, request.StartingPrice, request.ReservePrice,
            request.BidIncrement, request.BuyNowPrice,
            request.StartTime, request.EndTime, relistCount
        );

        ((Microsoft.EntityFrameworkCore.DbContext)_db).Add(auction);
        await _db.SaveChangesAsync(cancellationToken);

        return auction.Id;
    }
}