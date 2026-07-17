using System.Text.Json;
using AuctionPlatform.Domain.Entities;
using AuctionPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuctionPlatform.Infrastructure.Persistence.Seeders;

public class CategoryItemSeeder
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CategoryItemSeeder> _logger;
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CategoryItemSeeder(ApplicationDbContext db, ILogger<CategoryItemSeeder> logger, IHttpClientFactory httpFactory)
    {
        _db = db;
        _logger = logger;
        _http = httpFactory.CreateClient("DummyJson");
    }

    /// <summary>
    /// Fetch 50 produk dari DummyJSON, petakan ke Categories dan Items,
    /// lalu insert ke database. Idempotent: skip kalau data sudah ada.
    /// </summary>
    public async Task<(Dictionary<string, Guid> categoryMap, List<Item> items)> SeedAsync(
        Guid defaultSellerId, CancellationToken ct)
    {
        var categoryMap = new Dictionary<string, Guid>();
        var items = new List<Item>();

        if (await _db.Items.AnyAsync(ct))
        {
            _logger.LogInformation("[Seeder] Items sudah ada, skip CategoryItemSeeder.");
            // kembalikan data yang sudah ada untuk dipakai AuctionSeeder
            var existingCategories = await _db.Categories.ToListAsync(ct);
            foreach (var c in existingCategories) categoryMap[c.Name] = c.Id;
            items = await _db.Items.ToListAsync(ct);
            return (categoryMap, items);
        }

        _logger.LogInformation("[Seeder] Fetching 50 produk dari DummyJSON...");
        DummyJsonResponse? response = null;

        try
        {
            var json = await _http.GetStringAsync("https://dummyjson.com/products?limit=50&select=id,title,description,category,price,thumbnail,images", ct);
            response = JsonSerializer.Deserialize<DummyJsonResponse>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[Seeder] Gagal fetch DummyJSON ({Message}), pakai data fallback.", ex.Message);
        }

        var products = response?.Products ?? FallbackProducts();
        _logger.LogInformation("[Seeder] Memproses {Count} produk...", products.Count);

        // Buat Categories dari kategori unik yang ada di produk
        var uniqueCategories = products.Select(p => p.Category).Distinct().ToList();
        foreach (var catName in uniqueCategories)
        {
            var slug = catName.ToLower().Replace(" ", "-").Replace("/", "-");
            var category = Category.Create(CapitalizeWords(catName), slug);
            _db.Categories.Add(category);
            categoryMap[catName] = category.Id;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Seeder] {Count} kategori berhasil disimpan.", uniqueCategories.Count);

        // Buat Items dari produk
        foreach (var product in products)
        {
            if (!categoryMap.TryGetValue(product.Category, out var categoryId)) continue;

            var item = Item.Create(
                sellerId: defaultSellerId,
                categoryId: categoryId,
                title: product.Title,
                description: product.Description,
                condition: "Used"
            );

            // Tambah gambar: prioritaskan thumbnail, lalu images[0], lalu placeholder
            var imageUrl = !string.IsNullOrEmpty(product.Thumbnail)
                ? product.Thumbnail
                : product.Images.FirstOrDefault() ?? $"https://picsum.photos/seed/{product.Id}/400/300";

            item.AddImage(imageUrl, 0);

            // Tambah gambar tambahan kalau ada (max 3)
            foreach (var (img, idx) in product.Images.Take(3).Select((img, i) => (img, i + 1)))
                item.AddImage(img, idx);

            _db.Items.Add(item);
            items.Add(item);
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("[Seeder] {Count} item berhasil disimpan.", items.Count);

        return (categoryMap, items);
    }

    private static string CapitalizeWords(string s)
        => string.Join(" ", s.Split(' ').Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w[1..] : w));

    private static List<DummyJsonProduct> FallbackProducts() =>
    [
        new(1, "iPhone 14 Pro", "Smartphone flagship Apple dengan chip A16 Bionic.", "smartphones", 999, "https://picsum.photos/seed/1/400/300", []),
        new(2, "Samsung Galaxy S23", "Android flagship Samsung dengan kamera 200MP.", "smartphones", 799, "https://picsum.photos/seed/2/400/300", []),
        new(3, "MacBook Pro M2", "Laptop profesional Apple dengan chip M2.", "laptops", 1999, "https://picsum.photos/seed/3/400/300", []),
        new(4, "Sony WH-1000XM5", "Headphone noise-cancelling terbaik di kelasnya.", "electronics", 349, "https://picsum.photos/seed/4/400/300", []),
        new(5, "iPad Air 5", "Tablet Apple dengan chip M1 dan layar 10.9 inci.", "tablets", 599, "https://picsum.photos/seed/5/400/300", []),
    ];
}