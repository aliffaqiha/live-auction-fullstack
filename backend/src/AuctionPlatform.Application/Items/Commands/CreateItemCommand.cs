using AuctionPlatform.Application.Interfaces;
using AuctionPlatform.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuctionPlatform.Application.Items.Commands;

public record CreateItemCommand(
    Guid SellerId,
    Guid CategoryId,
    string Title,
    string Description,
    string Condition,
    List<string> ImageUrls
) : IRequest<Guid>;

public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, Guid>
{
    private readonly IApplicationDbContext _db;

    public CreateItemCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Guid> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
            throw new InvalidOperationException("Kategori tidak ditemukan.");
        // Guard sederhana: cegah submit ganda (misal user double-klik tombol
        // simpan, atau kirim request yang sama berulang dalam waktu singkat).
        var recentDuplicateExists = await _db.Items.AnyAsync(i =>
            i.SellerId == request.SellerId &&
            i.Title == request.Title &&
            i.CreatedAt >= DateTime.UtcNow.AddSeconds(-30),
            cancellationToken);

if (recentDuplicateExists)
    throw new InvalidOperationException(
        "Item dengan judul yang sama baru saja dibuat. Tunggu beberapa saat sebelum mencoba lagi.");

        var item = Item.Create(request.SellerId, request.CategoryId, request.Title, request.Description, request.Condition);

        if (request.ImageUrls.Count == 0)
            throw new InvalidOperationException("Minimal satu gambar diperlukan.");

        for (int i = 0; i < request.ImageUrls.Count; i++)
            item.AddImage(request.ImageUrls[i], i);

        var dbContext = (Microsoft.EntityFrameworkCore.DbContext)_db;
        dbContext.Add(item);

        foreach (var image in item.Images)
            dbContext.Add(image);

        await _db.SaveChangesAsync(cancellationToken);

        return item.Id;
    }
}