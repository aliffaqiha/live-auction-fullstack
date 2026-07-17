using AuctionPlatform.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Auctions.Commands;

public record ActivateAuctionCommand(Guid AuctionId) : IRequest;

public class ActivateAuctionCommandHandler : IRequestHandler<ActivateAuctionCommand>
{
    private readonly IApplicationDbContext _db;

    public ActivateAuctionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task Handle(ActivateAuctionCommand request, CancellationToken cancellationToken)
    {
        var auction = await _db.GetAuctionForUpdateAsync(request.AuctionId, cancellationToken);
        if (auction is null) return;

        if (auction.Status != Domain.Enums.AuctionStatus.Scheduled) return;

        auction.Activate(DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }
}