using FluentValidation;

namespace AuctionPlatform.Application.Auth.Commands;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password wajib diisi.")
            .MinimumLength(6).WithMessage("Password minimal 6 karakter.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nama lengkap wajib diisi.")
            .MaximumLength(200).WithMessage("Nama lengkap maksimal 200 karakter.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role wajib diisi.")
            .Must(role => new[] { "Bidder", "Seller", "Both" }.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Role harus salah satu dari: Bidder, Seller, Both.");
    }
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email wajib diisi.")
            .EmailAddress().WithMessage("Format email tidak valid.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password wajib diisi.");
    }
}