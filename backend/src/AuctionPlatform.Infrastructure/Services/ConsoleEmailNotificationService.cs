using AuctionPlatform.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuctionPlatform.Infrastructure.Services;

/// <summary>
/// Implementasi development: "kirim" email cukup dengan log terformat rapi
/// ke console, supaya gampang diverifikasi lewat `docker logs` tanpa perlu
/// setup SMTP server sungguhan. Ganti dengan SmtpEmailNotificationService
/// (atau provider lain) saat siap ke tahap produksi.
/// </summary>
public class ConsoleEmailNotificationService : IEmailNotificationService
{
    private readonly ILogger<ConsoleEmailNotificationService> _logger;

    public ConsoleEmailNotificationService(ILogger<ConsoleEmailNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAuctionWonEmailAsync(
        string toEmail, string bidderName, string itemTitle, decimal finalPrice, CancellationToken ct = default)
    {
        LogEmail(
            toEmail,
            subject: $"Selamat! Anda memenangkan lelang \"{itemTitle}\"",
            body:
                $"Halo {bidderName},\n\n" +
                $"Selamat, Anda memenangkan lelang untuk \"{itemTitle}\" dengan harga akhir {FormatIDR(finalPrice)}.\n" +
                $"Saldo Anda sudah otomatis terpotong. Silakan hubungi penjual untuk proses serah terima barang."
        );
        return Task.CompletedTask;
    }

    public Task SendOutbidEmailAsync(
        string toEmail, string bidderName, string itemTitle, decimal newHighestBid, CancellationToken ct = default)
    {
        LogEmail(
            toEmail,
            subject: $"Anda tersalip di lelang \"{itemTitle}\"",
            body:
                $"Halo {bidderName},\n\n" +
                $"Penawaran Anda untuk \"{itemTitle}\" baru saja tersalip. Penawaran tertinggi sekarang {FormatIDR(newHighestBid)}.\n" +
                $"Segera ajukan penawaran baru kalau masih berminat!"
        );
        return Task.CompletedTask;
    }

    public Task SendAuctionEndedSellerEmailAsync(
        string toEmail, string sellerName, string itemTitle, bool sold, decimal? finalPrice, CancellationToken ct = default)
    {
        var body = sold
            ? $"Halo {sellerName},\n\nLelang \"{itemTitle}\" telah berakhir dan TERJUAL seharga {FormatIDR(finalPrice ?? 0)}."
            : $"Halo {sellerName},\n\nLelang \"{itemTitle}\" telah berakhir TANPA terjual. Anda bisa melakukan relist kapan saja.";

        LogEmail(toEmail, subject: $"Lelang \"{itemTitle}\" telah berakhir", body: body);
        return Task.CompletedTask;
    }

    private void LogEmail(string toEmail, string subject, string body)
    {
        _logger.LogInformation(
            "\n" +
            "┌─────────────────────────────────────────────────────────\n" +
            "│ 📧 EMAIL (simulasi -- development mode)\n" +
            "│ To      : {ToEmail}\n" +
            "│ Subject : {Subject}\n" +
            "│ Body    :\n{Body}\n" +
            "└─────────────────────────────────────────────────────────",
            toEmail, subject, Indent(body)
        );
    }

    private static string Indent(string text)
        => string.Join("\n", text.Split('\n').Select(line => "│   " + line));

    private static string FormatIDR(decimal amount)
        => amount.ToString("C0", new System.Globalization.CultureInfo("id-ID"));
}