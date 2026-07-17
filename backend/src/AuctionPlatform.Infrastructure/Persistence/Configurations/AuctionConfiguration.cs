using AuctionPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuctionPlatform.Infrastructure.Persistence.Configurations;

public class AuctionConfiguration : IEntityTypeConfiguration<Auction>
{
    public void Configure(EntityTypeBuilder<Auction> builder)
    {
        builder.ToTable("Auctions");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.StartingPrice).HasColumnType("decimal(18,2)");
        builder.Property(a => a.ReservePrice).HasColumnType("decimal(18,2)");
        builder.Property(a => a.BidIncrement).HasColumnType("decimal(18,2)");
        builder.Property(a => a.BuyNowPrice).HasColumnType("decimal(18,2)");
        builder.Property(a => a.CurrentHighestBid).HasColumnType("decimal(18,2)");
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);

        // Memetakan system column "xmin" PostgreSQL sebagai concurrency token.
        // Ini memberi optimistic concurrency "gratis" tanpa perlu kolom version manual:
        // EF Core otomatis menambahkan "WHERE xmin = @originalXmin" di setiap UPDATE,
        // dan melempar DbUpdateConcurrencyException kalau row sudah berubah duluan.
        builder.Property(a => a.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasIndex(a => a.ItemId);
        builder.HasIndex(a => a.Status);
    }
}
