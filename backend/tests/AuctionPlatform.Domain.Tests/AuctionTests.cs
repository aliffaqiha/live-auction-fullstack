using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using Xunit;

namespace AuctionPlatform.Domain.Tests;

public class AuctionTests
{
    private static Auction CreateActiveAuction(
        decimal startingPrice = 100_000, decimal bidIncrement = 10_000,
        decimal? reservePrice = null, DateTime? now = null)
    {
        var baseTime = now ?? DateTime.UtcNow;
        var auction = Auction.Create(
            itemId: Guid.NewGuid(),
            startingPrice: startingPrice,
            reservePrice: reservePrice,
            bidIncrement: bidIncrement,
            buyNowPrice: null,
            startTime: baseTime.AddMinutes(-10),
            endTime: baseTime.AddHours(1)
        );
        auction.Activate(baseTime.AddMinutes(-10));
        return auction;
    }

    [Fact]
    public void Create_WithInvalidStartingPrice_ThrowsException()
    {
        var act = () => Auction.Create(
            Guid.NewGuid(), startingPrice: 0, reservePrice: null, bidIncrement: 1000,
            buyNowPrice: null, startTime: DateTime.UtcNow, endTime: DateTime.UtcNow.AddDays(1));

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithEndTimeBeforeStartTime_ThrowsException()
    {
        var now = DateTime.UtcNow;
        var act = () => Auction.Create(
            Guid.NewGuid(), startingPrice: 100_000, reservePrice: null, bidIncrement: 1000,
            buyNowPrice: null, startTime: now, endTime: now.AddHours(-1));

        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Create_ExceedingMaxRelistCount_ThrowsException()
    {
        var now = DateTime.UtcNow;
        var act = () => Auction.Create(
            Guid.NewGuid(), startingPrice: 100_000, reservePrice: null, bidIncrement: 1000,
            buyNowPrice: null, startTime: now, endTime: now.AddHours(1),
            relistCount: Auction.MaxRelistCount + 1);

        Assert.Throws<InvalidOperationException>(act);
    }

   [Fact]
    public void AcceptBid_FirstBidBelowStartingPrice_ThrowsException()
    {
        var auction = CreateActiveAuction(startingPrice: 100_000, bidIncrement: 10_000);
        var now = DateTime.UtcNow;

        var act = () => { auction.AcceptBid(Guid.NewGuid(), 50_000, now); };

        Assert.Throws<InvalidOperationException>(act);
    }
    [Fact]
    public void AcceptBid_FirstBidEqualToStartingPrice_Succeeds()
    {
        var auction = CreateActiveAuction(startingPrice: 100_000, bidIncrement: 10_000);
        var now = DateTime.UtcNow;

        var accepted = auction.AcceptBid(Guid.NewGuid(), 100_000, now);

        Assert.Equal(100_000, accepted);
        Assert.Equal(100_000, auction.CurrentHighestBid);
    }

    [Fact]
    public void AcceptBid_BelowMinimumIncrement_ThrowsException()
    {
        var auction = CreateActiveAuction(startingPrice: 100_000, bidIncrement: 10_000);
        var now = DateTime.UtcNow;
        auction.AcceptBid(Guid.NewGuid(), 100_000, now);

        var act = () => { auction.AcceptBid(Guid.NewGuid(), 105_000, now); };

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void AcceptBid_ValidIncrement_UpdatesHighestBidAndBidder()
    {
        var auction = CreateActiveAuction(startingPrice: 100_000, bidIncrement: 10_000);
        var bidderId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        auction.AcceptBid(Guid.NewGuid(), 100_000, now);
        auction.AcceptBid(bidderId, 110_000, now);

        Assert.Equal(110_000, auction.CurrentHighestBid);
        Assert.Equal(bidderId, auction.CurrentHighestBidderId);
    }

    [Fact]
    public void AcceptBid_OnInactiveAuction_ThrowsException()
    {
        var now = DateTime.UtcNow;
        var auction = Auction.Create(
            Guid.NewGuid(), startingPrice: 100_000, reservePrice: null, bidIncrement: 10_000,
            buyNowPrice: null, startTime: now.AddHours(1), endTime: now.AddHours(2));

        var act = () => { auction.AcceptBid(Guid.NewGuid(), 100_000, now); };

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void AcceptBid_AfterEndTime_ThrowsException()
    {
        var auction = CreateActiveAuction();
        var afterEnd = DateTime.UtcNow.AddHours(2);

        var act = () => { auction.AcceptBid(Guid.NewGuid(), 110_000, afterEnd); };

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void AcceptBid_WithinAntiSnipingWindow_ExtendsEndTime()
    {
        var now = DateTime.UtcNow;
        var auction = Auction.Create(
            Guid.NewGuid(), startingPrice: 100_000, reservePrice: null, bidIncrement: 10_000,
            buyNowPrice: null, startTime: now.AddMinutes(-10), endTime: now.AddSeconds(20)); // berakhir 20 detik lagi
        auction.Activate(now.AddMinutes(-10));

        var originalEndTime = auction.EndTime;
        auction.AcceptBid(Guid.NewGuid(), 100_000, now); // bid masuk dalam window anti-snipe (< 30 detik)

        Assert.True(auction.EndTime > originalEndTime, "EndTime seharusnya diperpanjang karena anti-sniping.");
    }

    [Fact]
    public void AcceptBid_OutsideAntiSnipingWindow_DoesNotExtendEndTime()
    {
        var auction = CreateActiveAuction(); // EndTime +1 jam dari sekarang, jauh di luar window 30 detik
        var originalEndTime = auction.EndTime;

        auction.AcceptBid(Guid.NewGuid(), 100_000, DateTime.UtcNow);

        Assert.Equal(originalEndTime, auction.EndTime);
    }

    [Fact]
    public void Close_WithNoBids_SetsStatusToUnsold()
    {
        var auction = CreateActiveAuction();

        auction.Close(DateTime.UtcNow.AddHours(2));

        Assert.Equal(AuctionStatus.Unsold, auction.Status);
    }

    [Fact]
    public void Close_WithBidsAndNoReservePrice_SetsStatusToSold()
    {
        var auction = CreateActiveAuction(startingPrice: 100_000, bidIncrement: 10_000);
        auction.AcceptBid(Guid.NewGuid(), 100_000, DateTime.UtcNow);

        auction.Close(DateTime.UtcNow.AddHours(2));

        Assert.Equal(AuctionStatus.Sold, auction.Status);
    }

    [Fact]
    public void Close_WithBidsBelowReservePrice_SetsStatusToUnsold()
    {
        var auction = CreateActiveAuction(startingPrice: 100_000, bidIncrement: 10_000, reservePrice: 500_000);
        auction.AcceptBid(Guid.NewGuid(), 100_000, DateTime.UtcNow); // di bawah reserve 500_000

        auction.Close(DateTime.UtcNow.AddHours(2));

        Assert.Equal(AuctionStatus.Unsold, auction.Status);
    }

    [Fact]
    public void Close_WithBidsMeetingReservePrice_SetsStatusToSold()
    {
        var auction = CreateActiveAuction(startingPrice: 100_000, bidIncrement: 10_000, reservePrice: 100_000);
        auction.AcceptBid(Guid.NewGuid(), 100_000, DateTime.UtcNow);

        auction.Close(DateTime.UtcNow.AddHours(2));

        Assert.Equal(AuctionStatus.Sold, auction.Status);
    }

    [Fact]
    public void Close_OnNonActiveAuction_ThrowsException()
    {
        var now = DateTime.UtcNow;
        var auction = Auction.Create(
            Guid.NewGuid(), startingPrice: 100_000, reservePrice: null, bidIncrement: 10_000,
            buyNowPrice: null, startTime: now.AddHours(1), endTime: now.AddHours(2));
        // Status masih Scheduled, belum Activate

        var act = () => auction.Close(now);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Cancel_OnActiveAuction_SetsStatusToCancelled()
    {
        var auction = CreateActiveAuction();

        auction.Cancel();

        Assert.Equal(AuctionStatus.Cancelled, auction.Status);
    }

    [Fact]
    public void Cancel_OnSoldAuction_ThrowsException()
    {
        var auction = CreateActiveAuction();
        auction.AcceptBid(Guid.NewGuid(), 100_000, DateTime.UtcNow);
        auction.Close(DateTime.UtcNow.AddHours(2));

        var act = () => auction.Cancel();

        Assert.Throws<InvalidOperationException>(act);
    }
}