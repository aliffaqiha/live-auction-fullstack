using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemImage> ItemImages => Set<ItemImage>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<AuctionResult> AuctionResults => Set<AuctionResult>();

    IQueryable<User> IApplicationDbContext.Users => Users;
    IQueryable<Wallet> IApplicationDbContext.Wallets => Wallets;
    IQueryable<WalletTransaction> IApplicationDbContext.WalletTransactions => WalletTransactions;
    IQueryable<Category> IApplicationDbContext.Categories => Categories;
    IQueryable<Item> IApplicationDbContext.Items => Items;
    IQueryable<ItemImage> IApplicationDbContext.ItemImages => ItemImages;  // ← tambahkan ini
    IQueryable<Auction> IApplicationDbContext.Auctions => Auctions;
    IQueryable<Bid> IApplicationDbContext.Bids => Bids;
    IQueryable<AuctionResult> IApplicationDbContext.AuctionResults => AuctionResults;

    public async Task<Auction?> GetAuctionForUpdateAsync(Guid auctionId, CancellationToken cancellationToken = default)
    {
        return await Auctions
            .FromSqlInterpolated($"SELECT *, xmin FROM \"Auctions\" WHERE \"Id\" = {auctionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<Wallet>().ToTable("Wallets");
        modelBuilder.Entity<WalletTransaction>().ToTable("WalletTransactions");
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Item>().ToTable("Items");
        modelBuilder.Entity<ItemImage>().ToTable("ItemImages");
        modelBuilder.Entity<Bid>().ToTable("Bids");
        modelBuilder.Entity<AuctionResult>().ToTable("AuctionResults");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}