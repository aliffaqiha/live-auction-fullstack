using AuctionPlatform.Application.Auctions.Commands;
using AuctionPlatform.Domain.Enums;
using AuctionPlatform.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuctionPlatform.Infrastructure.BackgroundServices;

/// <summary>
/// Simulasi aktivitas bidding supaya demo platform terasa "hidup" tanpa perlu
/// banyak orang buka browser bersamaan. Bot cuma memilih dari akun dummy hasil
/// seeder (email berakhiran @auction.com), TIDAK PERNAH menyentuh akun asli
/// yang didaftarkan lewat form Register (biasanya domain lain).
///
/// Sengaja dipisah dari AuctionLifecycleBackgroundService karena tanggung
/// jawabnya beda: yang satu menjaga siklus status auction (wajib ada), yang
/// ini murni kosmetik demo (opsional, bisa dimatikan lewat appsettings).
/// </summary>
public class BotBiddingBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BotBiddingBackgroundService> _logger;
    private readonly bool _enabled;

    public BotBiddingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BotBiddingBackgroundService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enabled = configuration.GetValue("BotBidding:Enabled", true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("[BotBidding] Dinonaktifkan lewat konfigurasi, service tidak berjalan.");
            return;
        }

        var rng = new Random();

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOnceAsync(rng, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BotBidding] Error saat memproses simulasi bidding.");
            }

            var delaySeconds = rng.Next(15, 46);
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(Random rng, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mediator = scope.ServiceProvider.GetRequiredService<ISender>();

        var now = DateTime.UtcNow;

        var activeAuctions = await db.Auctions
            .Where(a => a.Status == AuctionStatus.Active && a.EndTime > now.AddMinutes(1))
            .ToListAsync(ct);

        if (activeAuctions.Count == 0) return;

        var botCandidates = await db.Users
            .Where(u => u.Role == UserRole.Bidder && u.Email.EndsWith("@auction.com") && !u.IsDeleted)
            .ToListAsync(ct);

        if (botCandidates.Count == 0) return;

        var numberOfAuctionsToProcess = rng.Next(1, Math.Min(3, activeAuctions.Count) + 1);
        var selectedAuctions = activeAuctions
            .OrderBy(_ => rng.Next())
            .Take(numberOfAuctionsToProcess);

        foreach (var auction in selectedAuctions)
        {
            var item = await db.Items.FirstOrDefaultAsync(i => i.Id == auction.ItemId, ct);
            if (item is null) continue;

            var eligibleBots = botCandidates
                .Where(b => b.Id != item.SellerId && b.Id != auction.CurrentHighestBidderId)
                .ToList();

            if (eligibleBots.Count == 0) continue;

            var bot = eligibleBots[rng.Next(eligibleBots.Count)];
            var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == bot.Id, ct);
            if (wallet is null) continue;

            var minimumValid = auction.CurrentHighestBid.HasValue
                ? auction.CurrentHighestBid.Value + auction.BidIncrement
                : auction.StartingPrice;

            var extraSteps = rng.Next(0, 3);
            var bidAmount = minimumValid + auction.BidIncrement * extraSteps;

            if (wallet.AvailableBalance < bidAmount)
            {
                _logger.LogDebug(
                    "[BotBidding] Bot {Bot} saldo tidak cukup untuk auction {AuctionId}, skip.",
                    bot.Email, auction.Id);
                continue;
            }

            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            try
            {
                await mediator.Send(new PlaceBidCommand(auction.Id, bot.Id, bidAmount), ct);
                await transaction.CommitAsync(ct);
                _logger.LogInformation(
                    "[BotBidding] {Bot} mengajukan bid {Amount} ke auction {AuctionId}.",
                    bot.Email, bidAmount, auction.Id);
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogDebug(ex, "[BotBidding] Bid bot ditolak (kemungkinan race condition wajar).");
            }
        }
    }
}