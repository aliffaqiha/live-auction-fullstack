using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auctions.Queries;

// ── DTOs ──────────────────────────────────────────────────────────────────

public record MyBidHistoryDto(
    Guid AuctionId,
    string ItemTitle,
    string? ThumbnailUrl,
    decimal MyLastBidAmount,
    decimal? FinalPrice,
    string AuctionStatus,
    bool IsWinner,
    DateTime EndTime
);

public record MySellingHistoryDto(
    Guid AuctionId,
    Guid ItemId,
    string ItemTitle,
    string? ThumbnailUrl,
    decimal StartingPrice,
    decimal? FinalPrice,
    string Outcome,
    int TotalBids,
    DateTime EndTime
);

// ── GetMyBidHistoryQuery ──────────────────────────────────────────────────

public record GetMyBidHistoryQuery(Guid UserId) : IRequest<List<MyBidHistoryDto>>;

public class GetMyBidHistoryQueryHandler : IRequestHandler<GetMyBidHistoryQuery, List<MyBidHistoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMyBidHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MyBidHistoryDto>> Handle(GetMyBidHistoryQuery request, CancellationToken cancellationToken)
    {
        var auctionIds = await _db.Bids
            .Where(b => b.BidderId == request.UserId)
            .Select(b => b.AuctionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var result = new List<MyBidHistoryDto>();

        foreach (var auctionId in auctionIds)
        {
            var auction = await _db.Auctions.FirstOrDefaultAsync(a => a.Id == auctionId, cancellationToken);
            if (auction is null) continue;

            var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == auction.ItemId, cancellationToken);
            var thumbnail = await _db.ItemImages
                .Where(img => img.ItemId == auction.ItemId)
                .OrderBy(img => img.SortOrder)
                .Select(img => img.Url)
                .FirstOrDefaultAsync(cancellationToken);

            var myLastBid = await _db.Bids
                .Where(b => b.AuctionId == auctionId && b.BidderId == request.UserId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstAsync(cancellationToken);

            var auctionResult = await _db.AuctionResults
                .FirstOrDefaultAsync(r => r.AuctionId == auctionId, cancellationToken);

            var isWinner = auctionResult?.WinningBidId is not null &&
                await _db.Bids.AnyAsync(b => b.Id == auctionResult.WinningBidId && b.BidderId == request.UserId, cancellationToken);

            result.Add(new MyBidHistoryDto(
                auction.Id, item?.Title ?? "-", thumbnail,
                myLastBid.Amount, auctionResult?.FinalPrice,
                auction.Status.ToString(), isWinner, auction.EndTime
            ));
        }

        return result.OrderByDescending(r => r.EndTime).ToList();
    }
}

// ── GetMySellingHistoryQuery ──────────────────────────────────────────────

public record GetMySellingHistoryQuery(Guid SellerId) : IRequest<List<MySellingHistoryDto>>;

public class GetMySellingHistoryQueryHandler : IRequestHandler<GetMySellingHistoryQuery, List<MySellingHistoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMySellingHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<MySellingHistoryDto>> Handle(GetMySellingHistoryQuery request, CancellationToken cancellationToken)
    {
        var myItemIds = await _db.Items
            .Where(i => i.SellerId == request.SellerId)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        var auctions = await _db.Auctions
            .Where(a => myItemIds.Contains(a.ItemId) &&
                (a.Status == AuctionStatus.Sold || a.Status == AuctionStatus.Unsold))
            .OrderByDescending(a => a.EndTime)
            .ToListAsync(cancellationToken);

        var result = new List<MySellingHistoryDto>();

        foreach (var auction in auctions)
        {
            var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == auction.ItemId, cancellationToken);
            var thumbnail = await _db.ItemImages
                .Where(img => img.ItemId == auction.ItemId)
                .OrderBy(img => img.SortOrder)
                .Select(img => img.Url)
                .FirstOrDefaultAsync(cancellationToken);

            var auctionResult = await _db.AuctionResults
                .FirstOrDefaultAsync(r => r.AuctionId == auction.Id, cancellationToken);

            var totalBids = await _db.Bids.CountAsync(b => b.AuctionId == auction.Id, cancellationToken);

            result.Add(new MySellingHistoryDto(
                auction.Id, auction.ItemId, item?.Title ?? "-", thumbnail,
                auction.StartingPrice, auctionResult?.FinalPrice,
                auction.Status.ToString(), totalBids, auction.EndTime
            ));
        }

        return result;
    }
}