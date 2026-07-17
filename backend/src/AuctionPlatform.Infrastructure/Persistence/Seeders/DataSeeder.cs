using AuctionPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuctionPlatform.Infrastructure.Persistence.Seeders;

public class DataSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(IServiceScopeFactory scopeFactory, ILogger<DataSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Seeder] Memulai proses seeding data...");

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var httpFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();

        try
        {
            _logger.LogInformation("[Seeder] Menjalankan migration database...");
            await db.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("[Seeder] Migration selesai.");

            var categoryItemSeeder = new CategoryItemSeeder(
                db,
                scope.ServiceProvider.GetRequiredService<ILogger<CategoryItemSeeder>>(),
                httpFactory
            );

            var userAuctionSeeder = new UserAuctionSeeder(
                db,
                scope.ServiceProvider.GetRequiredService<ILogger<UserAuctionSeeder>>()
            );

            var botUserSeeder = new BotUserSeeder(
                db,
                scope.ServiceProvider.GetRequiredService<ILogger<BotUserSeeder>>()
            );

            // Step 1: seed user biasa (seller & bidder dummy)
            var users = await userAuctionSeeder.SeedUsersAsync(cancellationToken);

            // Step 2: seed bot user — setelah user biasa, sebelum item & auction
            // supaya context tracking tidak bermasalah
            await botUserSeeder.SeedAsync(cancellationToken);

            // Step 3: seed categories & items dari DummyJSON
            var defaultSeller = users.FirstOrDefault(u => u.Role == UserRole.Seller)
                ?? throw new InvalidOperationException("Tidak ada seller yang berhasil dibuat.");

            var (_, items) = await categoryItemSeeder.SeedAsync(defaultSeller.Id, cancellationToken);

            // Step 4: seed auctions dari items yang sudah ada
            await userAuctionSeeder.SeedAuctionsAsync(users, items, cancellationToken);

            _logger.LogInformation("[Seeder] Seeding selesai.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Seeder] Seeding gagal: {Message}", ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}