namespace AuctionPlatform.Infrastructure.Persistence.Seeders;

public record DummyJsonResponse(List<DummyJsonProduct> Products, int Total, int Skip, int Limit);

public record DummyJsonProduct(
    int Id,
    string Title,
    string Description,
    string Category,
    double Price,
    string Thumbnail,
    List<string> Images
);