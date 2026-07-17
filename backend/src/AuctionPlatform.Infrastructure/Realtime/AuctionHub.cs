using Microsoft.AspNetCore.SignalR;

namespace AuctionPlatform.Infrastructure.Realtime;

/// <summary>
/// Hub ini ditempatkan di Infrastructure (bukan API) supaya gampang dipindah
/// kalau nanti perlu di-scale ke project terpisah. Client connect ke
/// "/hubs/auction" lalu join group per auctionId untuk menerima update
/// hanya dari room lelang yang sedang mereka tonton.
/// </summary>
public class AuctionHub : Hub
{
    public async Task JoinAuctionRoom(string auctionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(auctionId));

    public async Task LeaveAuctionRoom(string auctionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(auctionId));

    public static string GroupName(string auctionId) => $"auction-{auctionId}";
}
