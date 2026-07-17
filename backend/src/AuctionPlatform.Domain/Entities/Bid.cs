using AuctionPlatform.Domain.Common;
using AuctionPlatform.Domain.Enums;

namespace AuctionPlatform.Domain.Entities;

public class Bid : BaseEntity
{
    public Guid AuctionId { get; private set; }
    public Guid BidderId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal? MaxProxyAmount { get; private set; }
    public BidStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Bid() { }

    public static Bid Create(Guid auctionId, Guid bidderId, decimal amount, decimal? maxProxyAmount = null) => new()
    {
        AuctionId = auctionId,
        BidderId = bidderId,
        Amount = amount,
        MaxProxyAmount = maxProxyAmount,
        Status = BidStatus.Valid
    };

    public void MarkAsOutbid() => Status = BidStatus.Outbid;
    public void MarkAsWinning() => Status = BidStatus.Winning;
    public void MarkAsRejected() => Status = BidStatus.Rejected;
}

public class AuctionResult : BaseEntity
{
    public Guid AuctionId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid? WinningBidId { get; private set; }
    public decimal? FinalPrice { get; private set; }
    public AuctionOutcome Outcome { get; private set; }
    public DateTime SettledAt { get; private set; } = DateTime.UtcNow;

    private AuctionResult() { }

    public static AuctionResult Create(Guid auctionId, Guid itemId, Guid? winningBidId, decimal? finalPrice, AuctionOutcome outcome)
        => new()
        {
            AuctionId = auctionId,
            ItemId = itemId,
            WinningBidId = winningBidId,
            FinalPrice = finalPrice,
            Outcome = outcome
        };
}
