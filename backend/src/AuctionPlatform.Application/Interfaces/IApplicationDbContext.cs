using AuctionPlatform.Domain.Entities;

namespace AuctionPlatform.Application.Interfaces;

/// <summary>
/// Application layer hanya tahu "interface", implementasi konkretnya
/// (EF Core + PostgreSQL) ada di Infrastructure layer. Ini inti dari
/// Dependency Inversion di Onion Architecture.
/// </summary>
public interface IApplicationDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<Wallet> Wallets { get; }
    IQueryable<WalletTransaction> WalletTransactions { get; }
    IQueryable<Category> Categories { get; }
    IQueryable<Item> Items { get; }
    IQueryable<ItemImage> ItemImages { get; }
    IQueryable<Auction> Auctions { get; }
    IQueryable<Bid> Bids { get; }
    IQueryable<AuctionResult> AuctionResults { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Mengunci row Auction (SELECT ... FOR UPDATE) di dalam transaction aktif,
    /// dipakai khusus saat proses PlaceBid untuk mencegah race condition.
    /// </summary>
    Task<Auction?> GetAuctionForUpdateAsync(Guid auctionId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraksi untuk broadcast real-time. Implementasi konkretnya (SignalR Hub)
/// ada di Infrastructure/API layer, supaya Application layer tidak perlu tahu
/// detail SignalR sama sekali.
/// </summary>
public interface IAuctionBroadcaster
{
    Task BroadcastNewBidAsync(Guid auctionId, decimal amount, Guid bidderId, DateTime endTime, CancellationToken ct = default);
    Task BroadcastOutbidAsync(Guid auctionId, Guid outbidUserId, CancellationToken ct = default);
    Task BroadcastAuctionEndedAsync(Guid auctionId, CancellationToken ct = default);
}

public interface ICurrentUserService
{
    Guid? UserId { get; }
}