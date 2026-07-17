using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionPlatform.Infrastructure.Persistence.Seeders;

public class BotUserSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<BotUserSeeder> _logger;

    private const string BotPasswordHash = "$2a$11$dummyhashforseeding.onlyfordevpurpose";

    // Email berakhiran @auction.com supaya BotBiddingBackgroundService
    // bisa membedakan "bot" dari user asli yang daftar lewat form Register
    private static readonly (string Email, string Name, decimal Saldo)[] BotUsers =
    {
        ("bidder1@auction.com",  "Bot Andi",    8_000_000m),
        ("bidder2@auction.com",  "Bot Maya",    6_000_000m),
        ("bidder3@auction.com",  "Bot Dian",    10_000_000m),
        ("bidder4@auction.com",  "Bot Fajar",   5_000_000m),
        ("bidder5@auction.com",  "Bot Rina",    12_000_000m),
        ("bidder6@auction.com",  "Bot Hendra",  4_000_000m),
        ("bidder7@auction.com",  "Bot Lina",    15_000_000m),
        ("bidder8@auction.com",  "Bot Rizky",   7_000_000m),
        ("bidder9@auction.com",  "Bot Nadia",   9_000_000m),
        ("bidder10@auction.com", "Bot Yoga",    11_000_000m)
    };

    public BotUserSeeder(ApplicationDbContext db, ILogger<BotUserSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Cek apakah minimal satu bot user sudah ada — kalau sudah, skip semua
        var alreadySeeded = await _db.Users
            .AnyAsync(u => u.Email.EndsWith("@auction.com"), ct);

        if (alreadySeeded)
        {
            _logger.LogInformation("[BotUserSeeder] Bot users sudah ada, skip.");
            return;
        }

        _logger.LogInformation("[BotUserSeeder] Membuat {Count} bot user...", BotUsers.Length);

        foreach (var (email, name, saldo) in BotUsers)
        {
            var user = User.Register(email, BotPasswordHash, name, UserRole.Bidder);
            user.MarkAsVerified();
            _db.Users.Add(user);

            var wallet = Wallet.CreateFor(user.Id);
            wallet.TopUp(saldo);
            _db.Wallets.Add(wallet);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[BotUserSeeder] {Count} bot user berhasil dibuat.", BotUsers.Length);
    }
}