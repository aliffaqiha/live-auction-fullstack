using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auctions.Queries;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record AuctionSummaryDto(
    Guid Id,
    Guid ItemId,
    string ItemTitle,
    string? ThumbnailUrl,
    string CategoryName,
    decimal StartingPrice,
    decimal? CurrentHighestBid,
    decimal BidIncrement,
    decimal? BuyNowPrice,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    int TotalBids
);

public record AuctionDetailDto(
    Guid Id,
    Guid ItemId,
    string ItemTitle,
    string ItemDescription,
    string ItemCondition,
    List<string> ImageUrls,
    string CategoryName,
    string SellerName,
    decimal StartingPrice,
    decimal? ReservePrice,
    decimal BidIncrement,
    decimal? BuyNowPrice,
    decimal? CurrentHighestBid,
    Guid? CurrentHighestBidderId,
    DateTime StartTime,
    DateTime EndTime,
    string Status,
    int RelistCount,
    List<BidHistoryDto> RecentBids
);

public record BidHistoryDto(
    Guid BidId,
    string BidderName,
    decimal Amount,
    DateTime PlacedAt,
    string Status
);

// ── GetAuctions (list dengan filter) ─────────────────────────────────────────

public record GetAuctionsQuery(
    string? Status = null,
    Guid? CategoryId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12
) : IRequest<GetAuctionsResult>;

public record GetAuctionsResult(List<AuctionSummaryDto> Items, int Total, int Page, int PageSize);

public class GetAuctionsQueryHandler : IRequestHandler<GetAuctionsQuery, GetAuctionsResult>
{
    private readonly IApplicationDbContext _db;

    public GetAuctionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<GetAuctionsResult> Handle(GetAuctionsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Auctions.AsQueryable();

        // Filter status
        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<AuctionStatus>(request.Status, true, out var status))
            query = query.Where(a => a.Status == status);
        else
            // Default: tampilkan yang Active dan Scheduled
            query = query.Where(a => a.Status == AuctionStatus.Active || a.Status == AuctionStatus.Scheduled);

        // Filter kategori
        if (request.CategoryId.HasValue)
            query = query.Where(a => _db.Items.Any(i => i.Id == a.ItemId && i.CategoryId == request.CategoryId));

        // Search judul item
        if (!string.IsNullOrWhiteSpace(request.Search))
            query = query.Where(a => _db.Items.Any(i => i.Id == a.ItemId &&
                i.Title.ToLower().Contains(request.Search.ToLower())));

        var total = await query.CountAsync(cancellationToken);

        var auctions = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var results = new List<AuctionSummaryDto>();

        foreach (var auction in auctions)
        {
            var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == auction.ItemId, cancellationToken);
            if (item is null) continue;

            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == item.CategoryId, cancellationToken);
            var thumbnail = await _db.ItemImages
                .Where(img => img.ItemId == item.Id)
                .OrderBy(img => img.SortOrder)
                .Select(img => img.Url)
                .FirstOrDefaultAsync(cancellationToken);

            var totalBids = await _db.Bids.CountAsync(b => b.AuctionId == auction.Id, cancellationToken);

            results.Add(new AuctionSummaryDto(
                auction.Id, item.Id, item.Title, thumbnail,
                category?.Name ?? "-", auction.StartingPrice,
                auction.CurrentHighestBid, auction.BidIncrement,
                auction.BuyNowPrice,
                auction.StartTime, auction.EndTime,
                auction.Status.ToString(), totalBids
            ));
        }

        return new GetAuctionsResult(results, total, request.Page, request.PageSize);
    }
}

// ── GetAuctionDetail ──────────────────────────────────────────────────────────

public record GetAuctionDetailQuery(Guid AuctionId) : IRequest<AuctionDetailDto>;

public class GetAuctionDetailQueryHandler : IRequestHandler<GetAuctionDetailQuery, AuctionDetailDto>
{
    private readonly IApplicationDbContext _db;

    public GetAuctionDetailQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AuctionDetailDto> Handle(GetAuctionDetailQuery request, CancellationToken cancellationToken)
    {
        var auction = await _db.Auctions
            .FirstOrDefaultAsync(a => a.Id == request.AuctionId, cancellationToken)
            ?? throw new InvalidOperationException("Lelang tidak ditemukan.");

        var item = await _db.Items.FirstOrDefaultAsync(i => i.Id == auction.ItemId, cancellationToken)
            ?? throw new InvalidOperationException("Item tidak ditemukan.");

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == item.CategoryId, cancellationToken);
        var seller = await _db.Users.FirstOrDefaultAsync(u => u.Id == item.SellerId, cancellationToken);

        var imageUrls = await _db.ItemImages
            .Where(img => img.ItemId == item.Id)
            .OrderBy(img => img.SortOrder)
            .Select(img => img.Url)
            .ToListAsync(cancellationToken);

        var recentBids = await _db.Bids
            .Where(b => b.AuctionId == auction.Id)
            .OrderByDescending(b => b.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        var bidHistory = new List<BidHistoryDto>();
        foreach (var bid in recentBids)
        {
            var bidder = await _db.Users.FirstOrDefaultAsync(u => u.Id == bid.BidderId, cancellationToken);
            bidHistory.Add(new BidHistoryDto(
                bid.Id,
                bidder?.FullName ?? "Unknown",
                bid.Amount,
                bid.CreatedAt,
                bid.Status.ToString()
            ));
        }

        return new AuctionDetailDto(
            auction.Id, item.Id, item.Title, item.Description, item.Condition,
            imageUrls, category?.Name ?? "-", seller?.FullName ?? "-",
            auction.StartingPrice, auction.ReservePrice, auction.BidIncrement,
            auction.BuyNowPrice, auction.CurrentHighestBid, auction.CurrentHighestBidderId,
            auction.StartTime, auction.EndTime, auction.Status.ToString(),
            auction.RelistCount, bidHistory
        );
    }
}