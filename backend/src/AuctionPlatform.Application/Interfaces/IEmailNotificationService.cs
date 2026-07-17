namespace AuctionPlatform.Application.Interfaces;

/// <summary>
/// Abstraksi notifikasi email. Implementasi saat ini cuma log ke console
/// (development), tapi arsitekturnya siap diganti ke SMTP asli (Mailtrap,
/// Gmail, SendGrid, dst) tanpa perlu ubah kode di command manapun --
/// cukup ganti registrasi DI di Infrastructure layer.
/// </summary>
public interface IEmailNotificationService
{
    Task SendAuctionWonEmailAsync(
        string toEmail, string bidderName, string itemTitle, decimal finalPrice,
        CancellationToken ct = default);

    Task SendOutbidEmailAsync(
        string toEmail, string bidderName, string itemTitle, decimal newHighestBid,
        CancellationToken ct = default);

    Task SendAuctionEndedSellerEmailAsync(
        string toEmail, string sellerName, string itemTitle, bool sold, decimal? finalPrice,
        CancellationToken ct = default);
}