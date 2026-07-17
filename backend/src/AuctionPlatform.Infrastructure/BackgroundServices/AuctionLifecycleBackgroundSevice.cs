using AuctionPlatform.Application.Auctions.Commands;
using AuctionPlatform.Domain.Enums;
using AuctionPlatform.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuctionPlatform.Infrastructure.BackgroundServices;

public class AuctionLifecycleBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionLifecycleBackgroundService> _logger;

    public AuctionLifecycleBackgroundService(
        IServiceScopeFactory scopeFactory, ILogger<AuctionLifecycleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        await ProcessOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessOnceAsync(stoppingToken);
        }
    }

    private async Task ProcessOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mediator = scope.ServiceProvider.GetRequiredService<ISender>();
            var now = DateTime.UtcNow;

            var toActivate = await db.Auctions
                .Where(a => a.Status == AuctionStatus.Scheduled && a.StartTime <= now)
                .Select(a => a.Id)
                .ToListAsync(ct);

            foreach (var auctionId in toActivate)
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    await mediator.Send(new ActivateAuctionCommand(auctionId), ct);
                    await tx.CommitAsync(ct);
                    _logger.LogInformation("[Lifecycle] Auction {Id} diaktifkan.", auctionId);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogError(ex, "[Lifecycle] Gagal mengaktifkan auction {Id}", auctionId);
                }
            }

            var toClose = await db.Auctions
                .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
                .Select(a => a.Id)
                .ToListAsync(ct);

            foreach (var auctionId in toClose)
            {
                await using var tx = await db.Database.BeginTransactionAsync(ct);
                try
                {
                    await mediator.Send(new CloseAuctionCommand(auctionId), ct);
                    await tx.CommitAsync(ct);
                    _logger.LogInformation("[Lifecycle] Auction {Id} ditutup (settlement selesai).", auctionId);
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    _logger.LogError(ex, "[Lifecycle] Gagal menutup auction {Id}", auctionId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Lifecycle] Error saat memproses siklus auction.");
        }
    }
}