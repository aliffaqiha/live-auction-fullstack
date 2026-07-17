using AuctionPlatform.Domain.Common;

namespace AuctionPlatform.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;

    private Category() { }

    public static Category Create(string name, string slug) => new() { Name = name, Slug = slug };
}

/// <summary>
/// Item adalah entitas independen dari Auction, supaya satu barang fisik bisa
/// di-relist ke beberapa lelang (kalau gagal terjual) dan punya riwayat harga.
/// </summary>
public class Item : BaseEntity
{
    public Guid SellerId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Condition { get; private set; } = default!;
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private readonly List<ItemImage> _images = new();
    public IReadOnlyCollection<ItemImage> Images => _images.AsReadOnly();

    private Item() { }

    public static Item Create(Guid sellerId, Guid categoryId, string title, string description, string condition)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Judul item tidak boleh kosong.", nameof(title));

        return new Item
        {
            SellerId = sellerId,
            CategoryId = categoryId,
            Title = title,
            Description = description,
            Condition = condition
        };
    }

    public void AddImage(string url, int sortOrder) => _images.Add(ItemImage.Create(Id, url, sortOrder));

    public void SoftDelete() => IsDeleted = true;
}

public class ItemImage : BaseEntity
{
    public Guid ItemId { get; private set; }
    public string Url { get; private set; } = default!;
    public int SortOrder { get; private set; }

    private ItemImage() { }

    public static ItemImage Create(Guid itemId, string url, int sortOrder) => new()
    {
        ItemId = itemId,
        Url = url,
        SortOrder = sortOrder
    };
}
