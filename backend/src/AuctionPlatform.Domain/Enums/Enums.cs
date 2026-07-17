namespace AuctionPlatform.Domain.Enums;

public enum UserRole
{
    Bidder,
    Seller,
    Both,
    Admin
}

public enum AuctionStatus
{
    Draft,
    Scheduled,
    Active,
    Ended,
    Sold,
    Unsold,
    Cancelled
}

public enum BidStatus
{
    Valid,
    Outbid,
    Rejected,
    Winning
}

public enum AuctionOutcome
{
    Sold,
    Unsold,
    Cancelled
}

public enum WalletTransactionType
{
    TopUp,
    Hold,
    Release,
    Deduct,
    Refund
}
