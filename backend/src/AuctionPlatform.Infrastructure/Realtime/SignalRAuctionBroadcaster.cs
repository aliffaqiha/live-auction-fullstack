using AuctionPlatform.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace AuctionPlatform.Infrastructure.Realtime;

public class SignalRAuctionBroadcaster : IAuctionBroadcaster
{
    private readonly IHubContext<AuctionHub> _hubContext;

    public SignalRAuctionBroadcaster(IHubContext<AuctionHub> hubContext) => _hubContext = hubContext;

    public Task BroadcastNewBidAsync(Guid auctionId, decimal amount, Guid bidderId, DateTime endTime, CancellationToken ct = default)
        => _hubContext.Clients.Group(AuctionHub.GroupName(auctionId.ToString()))
            .SendAsync("NewBid", new { auctionId, amount, bidderId, endTime }, ct);

    public Task BroadcastOutbidAsync(Guid auctionId, Guid outbidUserId, CancellationToken ct = default)
        => _hubContext.Clients.Group(AuctionHub.GroupName(auctionId.ToString()))
            .SendAsync("Outbid", new { auctionId, outbidUserId }, ct);

    public Task BroadcastAuctionEndedAsync(Guid auctionId, CancellationToken ct = default)
        => _hubContext.Clients.Group(AuctionHub.GroupName(auctionId.ToString()))
            .SendAsync("AuctionEnded", new { auctionId }, ct);
}
