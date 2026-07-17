using AuctionPlatform.Domain.Common;

namespace AuctionPlatform.Domain.Entities;

/// <summary>
/// Wallet menyimpan saldo user. Balance = saldo yang bisa dipakai.
/// HeldBalance = saldo yang sedang "ditahan" karena user jadi highest bidder
/// di satu atau lebih lelang aktif.
/// Version dipakai untuk optimistic concurrency control di EF Core (rowversion).
/// </summary>
public class Wallet : BaseEntity
{
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public decimal HeldBalance { get; private set; }
    public uint Version { get; private set; } // dipetakan ke xmin / concurrency token di EF Core
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private Wallet() { }

    public static Wallet CreateFor(Guid userId) => new()
    {
        UserId = userId,
        Balance = 0,
        HeldBalance = 0
    };

    public decimal AvailableBalance => Balance - HeldBalance;

    public void TopUp(decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Jumlah top up harus lebih dari 0.");
        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Hold(decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Jumlah hold harus lebih dari 0.");
        if (AvailableBalance < amount)
            throw new InvalidOperationException("Saldo tidak mencukupi untuk melakukan bid ini.");

        HeldBalance += amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Jumlah release harus lebih dari 0.");
        HeldBalance = Math.Max(0, HeldBalance - amount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deduct(decimal amount)
    {
        if (amount <= 0) throw new InvalidOperationException("Jumlah deduct harus lebih dari 0.");
        if (Balance < amount)
            throw new InvalidOperationException("Saldo tidak mencukupi untuk settlement.");

        Balance -= amount;
        HeldBalance = Math.Max(0, HeldBalance - amount);
        UpdatedAt = DateTime.UtcNow;
    }
}
