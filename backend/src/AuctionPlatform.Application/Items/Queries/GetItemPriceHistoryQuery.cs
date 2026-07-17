using AuctionPlatform.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Items.Queries;

public record PriceHistoryEntryDto(
    Guid AuctionId,
    decimal StartingPrice,
    decimal? FinalPrice,
    string Outcome,
    DateTime SettledAt,
    int RelistAttempt
);

public record GetItemPriceHistoryQuery(Guid ItemId) : IRequest<List<PriceHistoryEntryDto>>;

/// <summary>
/// Menampilkan riwayat harga sebuah item lintas beberapa kali lelang.
/// Berguna kalau item pernah gagal terjual (Unsold) lalu di-relist --
/// pembeli/calon bidder bisa lihat "barang ini sudah 2x dilelang sebelumnya".
/// </summary>
public class GetItemPriceHistoryQueryHandler : IRequestHandler<GetItemPriceHistoryQuery, List<PriceHistoryEntryDto>>
{
    private readonly IApplicationDbContext _db;

    public GetItemPriceHistoryQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<PriceHistoryEntryDto>> Handle(GetItemPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        var results = await _db.AuctionResults
            .Where(r => r.ItemId == request.ItemId)
            .OrderBy(r => r.SettledAt)
            .ToListAsync(cancellationToken);

        var history = new List<PriceHistoryEntryDto>();
        int attempt = 1;

        foreach (var r in results)
        {
            var auction = await _db.Auctions.FirstOrDefaultAsync(a => a.Id == r.AuctionId, cancellationToken);
            if (auction is null) continue;

            history.Add(new PriceHistoryEntryDto(
                r.AuctionId, auction.StartingPrice, r.FinalPrice,
                r.Outcome.ToString(), r.SettledAt, attempt
            ));
            attempt++;
        }

        return history;
    }
}