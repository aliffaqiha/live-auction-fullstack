using AuctionPlatform.Domain.Common;
using AuctionPlatform.Domain.Enums;

namespace AuctionPlatform.Domain.Entities;

public class Auction : BaseEntity
{
    private static readonly TimeSpan AntiSnipingWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan AntiSnipingExtension = TimeSpan.FromMinutes(2);
    public const int MaxRelistCount = 3;

    public Guid ItemId { get; private set; }
    public decimal StartingPrice { get; private set; }
    public decimal? ReservePrice { get; private set; }
    public decimal BidIncrement { get; private set; }
    public decimal? BuyNowPrice { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public AuctionStatus Status { get; private set; }
    public int RelistCount { get; private set; }
    public decimal? CurrentHighestBid { get; private set; }
    public Guid? CurrentHighestBidderId { get; private set; }
    public uint Version { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Auction() { }

    public static Auction Create(
        Guid itemId, decimal startingPrice, decimal? reservePrice, decimal bidIncrement,
        decimal? buyNowPrice, DateTime startTime, DateTime endTime, int relistCount = 0)
    {
        if (startingPrice <= 0)
            throw new ArgumentException("Starting price harus lebih dari 0.");
        if (endTime <= startTime)
            throw new ArgumentException("End time harus setelah start time.");
        if (relistCount > MaxRelistCount)
            throw new InvalidOperationException($"Item ini sudah mencapai batas maksimal relist ({MaxRelistCount}x).");

        return new Auction
        {
            ItemId = itemId,
            StartingPrice = startingPrice,
            ReservePrice = reservePrice,
            BidIncrement = bidIncrement,
            BuyNowPrice = buyNowPrice,
            StartTime = startTime,
            EndTime = endTime,
            Status = AuctionStatus.Scheduled,
            RelistCount = relistCount
        };
    }

    public void Activate(DateTime now)
    {
        if (Status != AuctionStatus.Scheduled)
            throw new InvalidOperationException("Hanya lelang berstatus Scheduled yang bisa diaktifkan.");
        Status = AuctionStatus.Active;
    }

    /// <summary>
    /// Validasi dan terapkan bid baru. Dipanggil di dalam DB transaction dengan
    /// row sudah di-lock (FOR UPDATE) oleh Infrastructure layer, sehingga
    /// CurrentHighestBid yang dibaca di sini dijamin yang paling baru.
    /// </summary>
    public decimal AcceptBid(Guid bidderId, decimal amount, DateTime now)
    {
        if (Status != AuctionStatus.Active)
            throw new InvalidOperationException("Lelang tidak sedang aktif.");
        if (now < StartTime || now >= EndTime)
            throw new InvalidOperationException("Di luar waktu lelang.");

        var minimumValid = (CurrentHighestBid ?? StartingPrice - BidIncrement) + BidIncrement;
        if (amount < minimumValid)
            throw new InvalidOperationException($"Bid minimal adalah {minimumValid}.");

        CurrentHighestBid = amount;
        CurrentHighestBidderId = bidderId;

        // Anti-sniping: kalau bid masuk dalam window terakhir, perpanjang waktu lelang
        if (EndTime - now <= AntiSnipingWindow)
        {
            EndTime = EndTime.Add(AntiSnipingExtension);
        }

        return amount;
    }

    public void Close(DateTime now)
    {
        if (Status != AuctionStatus.Active)
            throw new InvalidOperationException("Hanya lelang Active yang bisa ditutup.");

        if (CurrentHighestBidderId is null)
        {
            Status = AuctionStatus.Unsold;
            return;
        }

        var meetsReserve = ReservePrice is null || CurrentHighestBid >= ReservePrice;
        Status = meetsReserve ? AuctionStatus.Sold : AuctionStatus.Unsold;
    }

    public void Cancel()
    {
        if (Status is AuctionStatus.Sold or AuctionStatus.Cancelled)
            throw new InvalidOperationException("Lelang yang sudah selesai/dibatalkan tidak bisa dibatalkan lagi.");
        Status = AuctionStatus.Cancelled;
    }

    public void BuyNow(Guid bidderId, DateTime now)
    {
        if (Status != AuctionStatus.Active)
            throw new InvalidOperationException("Lelang tidak sedang aktif.");
        if (BuyNowPrice is null)
            throw new InvalidOperationException("Lelang ini tidak memiliki opsi Beli Sekarang.");
        if (now < StartTime || now >= EndTime)
            throw new InvalidOperationException("Di luar waktu lelang.");
        if (CurrentHighestBid.HasValue && CurrentHighestBid >= BuyNowPrice)
            throw new InvalidOperationException("Sudah ada penawaran yang menyamai atau melebihi harga Beli Sekarang.");

        CurrentHighestBid = BuyNowPrice;
        CurrentHighestBidderId = bidderId;
        Status = AuctionStatus.Sold;
        EndTime = now;
    }
}
