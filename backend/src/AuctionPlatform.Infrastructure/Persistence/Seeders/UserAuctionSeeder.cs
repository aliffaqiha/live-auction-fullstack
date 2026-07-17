using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionPlatform.Infrastructure.Persistence.Seeders;

public class UserAuctionSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UserAuctionSeeder> _logger;

    // Password hash untuk "Password123!" - pakai BCrypt di production,
    // untuk seeder ini pakai placeholder yang bisa diganti nanti
    private const string DummyPasswordHash = "$2a$11$dummyhashforseeding.onlyfordevpurpose";

    public UserAuctionSeeder(ApplicationDbContext db, ILogger<UserAuctionSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<User>> SeedUsersAsync(CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(ct))
        {
            _logger.LogInformation("[Seeder] Users sudah ada, skip UserSeeder.");
            return await _db.Users.ToListAsync(ct);
        }

        var users = new List<User>();

        // 5 Seller
        var sellers = new[]
        {
            ("seller1@auction.com", "Budi Santoso"),
            ("seller2@auction.com", "Siti Rahayu"),
            ("seller3@auction.com", "Ahmad Fauzi"),
            ("seller4@auction.com", "Dewi Lestari"),
            ("seller5@auction.com", "Rudi Hermawan"),
        };

        foreach (var (email, name) in sellers)
        {
            var user = User.Register(email, DummyPasswordHash, name, UserRole.Seller);
            user.MarkAsVerified();
            _db.Users.Add(user);

            var wallet = Wallet.CreateFor(user.Id);
            // Seller mulai dengan saldo 0, tidak perlu TopUp
            _db.Wallets.Add(wallet);

            users.Add(user);
        }

        // 10 Bidder dengan saldo bervariasi antara 2jt - 20jt
        var bidders = new[]
        {
            ("bidder1@auction.com",  "Andi Wijaya",      5_000_000m),
            ("bidder2@auction.com",  "Maya Putri",       7_500_000m),
            ("bidder3@auction.com",  "Dian Pratama",     10_000_000m),
            ("bidder4@auction.com",  "Fajar Nugroho",    3_000_000m),
            ("bidder5@auction.com",  "Rina Susanti",     15_000_000m),
            ("bidder6@auction.com",  "Hendra Saputra",   2_500_000m),
            ("bidder7@auction.com",  "Lina Marlina",     20_000_000m),
            ("bidder8@auction.com",  "Rizky Firmansyah", 8_000_000m),
            ("bidder9@auction.com",  "Nadia Permata",    4_500_000m),
            ("bidder10@auction.com", "Yoga Prasetyo",    12_000_000m),
        };

        foreach (var (email, name, saldo) in bidders)
        {
            var user = User.Register(email, DummyPasswordHash, name, UserRole.Bidder);
            user.MarkAsVerified();
            _db.Users.Add(user);

            var wallet = Wallet.CreateFor(user.Id);
            wallet.TopUp(saldo);
            _db.Wallets.Add(wallet);

            users.Add(user);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Seeder] {Count} user + wallet berhasil disimpan.", users.Count);

        return users;
    }

    public async Task SeedAuctionsAsync(List<User> users, List<Item> items, CancellationToken ct)
    {
        if (await _db.Auctions.AnyAsync(ct))
        {
            _logger.LogInformation("[Seeder] Auctions sudah ada, skip AuctionSeeder.");
            return;
        }

        var now = DateTime.UtcNow;
        var sellers = users.Where(u => u.Role == UserRole.Seller).ToList();
        var bidders = users.Where(u => u.Role == UserRole.Bidder).ToList();
        var rng = new Random(42); // seed tetap supaya data konsisten tiap restart

        // Distribusi 30 auction dari 50 item:
        // 12 Active (bisa langsung di-bid)
        // 8  Scheduled (belum mulai)
        // 5  Ended + Sold (ada pemenang, untuk riwayat)
        // 5  Ended + Unsold (reserve price tidak tercapai)

        var auctionConfigs = new List<(Item item, AuctionStatus status, TimeSpan start, TimeSpan end, bool hasReserve)>();

        var shuffledItems = items.OrderBy(_ => rng.Next()).ToList();
        int idx = 0;

        // 12 Active
        for (int i = 0; i < 12 && idx < shuffledItems.Count; i++, idx++)
            auctionConfigs.Add((shuffledItems[idx], AuctionStatus.Active,
                TimeSpan.FromHours(-rng.Next(1, 24)),    // mulai 1-24 jam lalu
                TimeSpan.FromHours(rng.Next(2, 72)),     // berakhir 2-72 jam lagi
                false));

        // 8 Scheduled
        for (int i = 0; i < 8 && idx < shuffledItems.Count; i++, idx++)
            auctionConfigs.Add((shuffledItems[idx], AuctionStatus.Scheduled,
                TimeSpan.FromHours(rng.Next(2, 48)),     // mulai 2-48 jam lagi
                TimeSpan.FromHours(rng.Next(50, 120)),   // berakhir setelah itu
                false));

        // 5 Sold
        for (int i = 0; i < 5 && idx < shuffledItems.Count; i++, idx++)
            auctionConfigs.Add((shuffledItems[idx], AuctionStatus.Sold,
                TimeSpan.FromDays(-rng.Next(3, 14)),     // mulai 3-14 hari lalu
                TimeSpan.FromDays(-rng.Next(1, 2)),      // berakhir 1-2 hari lalu
                false));

        // 5 Unsold
        for (int i = 0; i < 5 && idx < shuffledItems.Count; i++, idx++)
            auctionConfigs.Add((shuffledItems[idx], AuctionStatus.Unsold,
                TimeSpan.FromDays(-rng.Next(3, 14)),
                TimeSpan.FromDays(-rng.Next(1, 2)),
                true)); // punya reserve price yang tidak tercapai

        foreach (var (item, targetStatus, startOffset, endOffset, hasReserve) in auctionConfigs)
        {
            var seller = sellers[rng.Next(sellers.Count)];

            // Harga awal antara 100rb - 5jt, kelipatan 50rb
            var startingPrice = Math.Round((decimal)(rng.Next(2, 100) * 50_000), 0);
            var bidIncrement = startingPrice switch
            {
                < 500_000 => 25_000m,
                < 2_000_000 => 50_000m,
                _ => 100_000m
            };
            var reservePrice = hasReserve ? startingPrice * (decimal)(1.5 + rng.NextDouble()) : (decimal?)null;

            var auction = Auction.Create(
                itemId: item.Id,
                startingPrice: startingPrice,
                reservePrice: reservePrice,
                bidIncrement: bidIncrement,
                buyNowPrice: null,
                startTime: now + startOffset,
                endTime: now + startOffset + (endOffset - startOffset),
                relistCount: 0
            );

            // Override status sesuai skenario
            if (targetStatus == AuctionStatus.Active)
                auction.Activate(now);
            else if (targetStatus is AuctionStatus.Sold or AuctionStatus.Unsold)
            {
                auction.Activate(now + startOffset);
                auction.Close(now + startOffset + (endOffset - startOffset));
            }

            _db.Auctions.Add(auction);

            // Untuk auction Sold: buat bid pemenang dan AuctionResult
            if (targetStatus == AuctionStatus.Sold)
            {
                var winner = bidders[rng.Next(bidders.Count)];
                var winningAmount = startingPrice + bidIncrement * rng.Next(3, 15);

                var winningBid = Bid.Create(auction.Id, winner.Id, winningAmount);
                winningBid.MarkAsWinning();
                _db.Bids.Add(winningBid);

                await _db.SaveChangesAsync(ct);

                var result = AuctionResult.Create(
                    auctionId: auction.Id,
                    itemId: item.Id,
                    winningBidId: winningBid.Id,
                    finalPrice: winningAmount,
                    outcome: AuctionOutcome.Sold
                );
                _db.AuctionResults.Add(result);
            }
            else if (targetStatus == AuctionStatus.Unsold)
            {
                await _db.SaveChangesAsync(ct);
                var result = AuctionResult.Create(
                    auctionId: auction.Id,
                    itemId: item.Id,
                    winningBidId: null,
                    finalPrice: null,
                    outcome: AuctionOutcome.Unsold
                );
                _db.AuctionResults.Add(result);
            }

            // Untuk Active auction: buat beberapa bid history supaya ada data
            if (targetStatus == AuctionStatus.Active)
            {
                var numBids = rng.Next(0, 8);
                var currentAmount = startingPrice;
                for (int b = 0; b < numBids; b++)
                {
                    currentAmount += bidIncrement;
                    var bidder = bidders[rng.Next(bidders.Count)];
                    var bid = Bid.Create(auction.Id, bidder.Id, currentAmount);
                    if (b == numBids - 1)
                        bid.MarkAsWinning();
                    else
                        bid.MarkAsOutbid();
                    _db.Bids.Add(bid);
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Seeder] {Count} auction berhasil disimpan.", auctionConfigs.Count);
    }
}