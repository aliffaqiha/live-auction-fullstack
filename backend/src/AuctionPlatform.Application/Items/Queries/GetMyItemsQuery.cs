using AuctionPlatform.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Items.Queries;

public record ItemSummaryDto(
    Guid Id,
    string Title,
    string CategoryName,
    string Condition,
    string? ThumbnailUrl,
    bool HasActiveAuction,
    DateTime CreatedAt
);

public record CategoryDto(Guid Id, string Name, string Slug);

public record ItemAuctionDto(
    Guid Id,
    string Status,
    decimal StartingPrice,
    decimal? CurrentHighestBid,
    DateTime StartTime,
    DateTime EndTime
);

public record GetMyItemsQuery(Guid SellerId) : IRequest<List<ItemSummaryDto>>;

public class GetMyItemsQueryHandler : IRequestHandler<GetMyItemsQuery, List<ItemSummaryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetMyItemsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ItemSummaryDto>> Handle(GetMyItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _db.Items
            .Where(i => i.SellerId == request.SellerId && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<ItemSummaryDto>();

        foreach (var item in items)
        {
            var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == item.CategoryId, cancellationToken);
            var thumbnail = await _db.ItemImages
                .Where(img => img.ItemId == item.Id)
                .OrderBy(img => img.SortOrder)
                .Select(img => img.Url)
                .FirstOrDefaultAsync(cancellationToken);

            var hasActiveAuction = await _db.Auctions.AnyAsync(
                a => a.ItemId == item.Id &&
                (a.Status == Domain.Enums.AuctionStatus.Active || a.Status == Domain.Enums.AuctionStatus.Scheduled),
                cancellationToken);

            result.Add(new ItemSummaryDto(
                item.Id, item.Title, category?.Name ?? "-", item.Condition,
                thumbnail, hasActiveAuction, item.CreatedAt
            ));
        }

        return result;
    }
}

public record GetCategoriesQuery() : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetCategoriesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Slug))
            .ToListAsync(cancellationToken);
    }
}

public record GetItemAuctionsQuery(Guid ItemId) : IRequest<List<ItemAuctionDto>>;

public class GetItemAuctionsQueryHandler : IRequestHandler<GetItemAuctionsQuery, List<ItemAuctionDto>>
{
    private readonly IApplicationDbContext _db;

    public GetItemAuctionsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<ItemAuctionDto>> Handle(GetItemAuctionsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Auctions
            .Where(a => a.ItemId == request.ItemId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ItemAuctionDto(
                a.Id, a.Status.ToString(), a.StartingPrice,
                a.CurrentHighestBid, a.StartTime, a.EndTime))
            .ToListAsync(cancellationToken);
    }
}