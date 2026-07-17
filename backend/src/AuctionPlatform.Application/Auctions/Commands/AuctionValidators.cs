using FluentValidation;

namespace AuctionPlatform.Application.Auctions.Commands;

public class PlaceBidCommandValidator : AbstractValidator<PlaceBidCommand>
{
    public PlaceBidCommandValidator()
    {
        RuleFor(x => x.AuctionId)
            .NotEmpty().WithMessage("AuctionId wajib diisi.");

        RuleFor(x => x.BidderId)
            .NotEmpty().WithMessage("BidderId wajib diisi.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Jumlah bid harus lebih dari 0.");
    }
}

public class CreateAuctionCommandValidator : AbstractValidator<CreateAuctionCommand>
{
    public CreateAuctionCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty().WithMessage("ItemId wajib diisi.");

        RuleFor(x => x.SellerId)
            .NotEmpty().WithMessage("SellerId wajib diisi.");

        RuleFor(x => x.StartingPrice)
            .GreaterThan(0).WithMessage("Harga awal harus lebih dari 0.");

        RuleFor(x => x.BidIncrement)
            .GreaterThan(0).WithMessage("Kelipatan bid harus lebih dari 0.");

        RuleFor(x => x.ReservePrice)
            .GreaterThanOrEqualTo(x => x.StartingPrice)
            .When(x => x.ReservePrice.HasValue)
            .WithMessage("Reserve price tidak boleh lebih kecil dari harga awal.");

        RuleFor(x => x.BuyNowPrice)
            .GreaterThan(x => x.StartingPrice)
            .When(x => x.BuyNowPrice.HasValue)
            .WithMessage("Buy Now price harus lebih besar dari harga awal.");

        RuleFor(x => x.EndTime)
            .GreaterThan(x => x.StartTime)
            .WithMessage("Waktu berakhir harus setelah waktu mulai.");

        RuleFor(x => x.StartTime)
            .GreaterThanOrEqualTo(DateTime.UtcNow.AddMinutes(-1))
            .WithMessage("Waktu mulai tidak boleh di masa lalu.");
    }
}