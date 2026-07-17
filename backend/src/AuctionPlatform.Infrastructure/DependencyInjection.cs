using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Infrastructure.BackgroundServices;
using AuctionPlatform.Infrastructure.Persistence;
using AuctionPlatform.Infrastructure.Persistence.Seeders;
using AuctionPlatform.Infrastructure.Realtime;
using AuctionPlatform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuctionPlatform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAuctionBroadcaster, SignalRAuctionBroadcaster>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IEmailNotificationService, ConsoleEmailNotificationService>();

        services.AddSignalR();

        services.AddHttpClient("DummyJson", client =>
        {
            client.BaseAddress = new Uri("https://dummyjson.com");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHostedService<DataSeeder>();
        services.AddHostedService<AuctionLifecycleBackgroundService>();
        services.AddHostedService<BotBiddingBackgroundService>();

        return services;
    }
}