using AuctionPlatform.Domain.Common;
using AuctionPlatform.Domain.Enums;

namespace AuctionPlatform.Domain.Entities;

public class WalletTransaction : BaseEntity
{
    public Guid WalletId { get; private set; }
    public WalletTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string? ReferenceId { get; private set; } // misal AuctionId/BidId terkait
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private WalletTransaction() { }

    public static WalletTransaction Create(Guid walletId, WalletTransactionType type, decimal amount, string? referenceId = null)
        => new()
        {
            WalletId = walletId,
            Type = type,
            Amount = amount,
            ReferenceId = referenceId
        };
}
