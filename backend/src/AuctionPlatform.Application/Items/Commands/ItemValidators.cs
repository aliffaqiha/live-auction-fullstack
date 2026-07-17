using FluentValidation;

namespace AuctionPlatform.Application.Items.Commands;

public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
{
    public CreateItemCommandValidator()
    {
        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("SellerId wajib diisi.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Kategori wajib dipilih.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Judul item wajib diisi.")
            .MaximumLength(200).WithMessage("Judul item maksimal 200 karakter.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Deskripsi wajib diisi.")
            .MaximumLength(2000).WithMessage("Deskripsi maksimal 2000 karakter.");

        RuleFor(x => x.Condition)
            .NotEmpty().WithMessage("Kondisi barang wajib diisi.");

        RuleFor(x => x.ImageUrls)
            .NotEmpty().WithMessage("Minimal satu gambar diperlukan.");

        RuleForEach(x => x.ImageUrls)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("URL gambar tidak valid.");
    }
}