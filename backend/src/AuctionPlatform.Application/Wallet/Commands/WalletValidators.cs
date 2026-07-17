using FluentValidation;

namespace AuctionPlatform.Application.Wallets.Commands;

public class TopUpWalletCommandValidator : AbstractValidator<TopUpWalletCommand>
{
    public TopUpWalletCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId wajib diisi.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Jumlah top up harus lebih dari 0.")
            .LessThanOrEqualTo(100_000_000).WithMessage("Jumlah top up maksimal 100.000.000 per transaksi.");
    }
}