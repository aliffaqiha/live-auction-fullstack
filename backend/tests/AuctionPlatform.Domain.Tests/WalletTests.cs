using AuctionPlatform.Domain.Entities;
using Xunit;

namespace AuctionPlatform.Domain.Tests;

public class WalletTests
{
    [Fact]
    public void CreateFor_NewWallet_HasZeroBalance()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());

        Assert.Equal(0, wallet.Balance);
        Assert.Equal(0, wallet.HeldBalance);
        Assert.Equal(0, wallet.AvailableBalance);
    }

    [Fact]
    public void TopUp_WithPositiveAmount_IncreasesBalance()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());

        wallet.TopUp(1_000_000);

        Assert.Equal(1_000_000, wallet.Balance);
        Assert.Equal(1_000_000, wallet.AvailableBalance);
    }

    [Fact]
    public void TopUp_WithZeroOrNegativeAmount_ThrowsException()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => wallet.TopUp(0));
        Assert.Throws<InvalidOperationException>(() => wallet.TopUp(-100));
    }

    [Fact]
    public void Hold_WithSufficientBalance_ReducesAvailableBalance()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(1_000_000);

        wallet.Hold(300_000);

        Assert.Equal(1_000_000, wallet.Balance); // Balance total tidak berubah
        Assert.Equal(300_000, wallet.HeldBalance);
        Assert.Equal(700_000, wallet.AvailableBalance); // Yang berkurang cuma available
    }

    [Fact]
    public void Hold_ExceedingAvailableBalance_ThrowsException()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(500_000);

        var act = () => wallet.Hold(600_000);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Hold_WithZeroOrNegativeAmount_ThrowsException()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(1_000_000);

        Assert.Throws<InvalidOperationException>(() => wallet.Hold(0));
        Assert.Throws<InvalidOperationException>(() => wallet.Hold(-100));
    }

    [Fact]
    public void Hold_TwiceExceedingCombinedAvailable_ThrowsExceptionOnSecondHold()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(1_000_000);

        wallet.Hold(700_000); // available sekarang 300_000

        var act = () => wallet.Hold(400_000); // melebihi sisa available

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Release_AfterHold_RestoresAvailableBalance()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(1_000_000);
        wallet.Hold(300_000);

        wallet.Release(300_000);

        Assert.Equal(1_000_000, wallet.Balance);
        Assert.Equal(0, wallet.HeldBalance);
        Assert.Equal(1_000_000, wallet.AvailableBalance);
    }

    [Fact]
    public void Release_MoreThanHeldBalance_ClampsToZero()
    {
        // Defensive behavior: release yang lebih besar dari held tidak boleh
        // membuat HeldBalance jadi negatif (bisa terjadi kalau ada race condition
        // atau data tidak konsisten -- lebih baik clamp daripada crash).
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(1_000_000);
        wallet.Hold(200_000);

        wallet.Release(500_000); // lebih besar dari yang di-hold

        Assert.Equal(0, wallet.HeldBalance);
    }

    [Fact]
    public void Deduct_AfterHold_ReducesBothBalanceAndHeldBalance()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(1_000_000);
        wallet.Hold(300_000);

        wallet.Deduct(300_000);

        Assert.Equal(700_000, wallet.Balance); // Balance permanen berkurang
        Assert.Equal(0, wallet.HeldBalance); // Held juga ikut berkurang/lunas
        Assert.Equal(700_000, wallet.AvailableBalance);
    }

    [Fact]
    public void Deduct_ExceedingBalance_ThrowsException()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(500_000);

        var act = () => wallet.Deduct(600_000);

        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Deduct_WithZeroOrNegativeAmount_ThrowsException()
    {
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(1_000_000);

        Assert.Throws<InvalidOperationException>(() => wallet.Deduct(0));
        Assert.Throws<InvalidOperationException>(() => wallet.Deduct(-100));
    }

    [Fact]
    public void FullBiddingLifecycle_HoldThenDeduct_ResultsInCorrectFinalBalance()
    {
        // Simulasi skenario nyata: top up -> bid (hold) -> menang lelang (deduct)
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(5_000_000);

        wallet.Hold(1_000_000); // ajukan bid
        wallet.Deduct(1_000_000); // menang, saldo benar-benar terpotong

        Assert.Equal(4_000_000, wallet.Balance);
        Assert.Equal(0, wallet.HeldBalance);
        Assert.Equal(4_000_000, wallet.AvailableBalance);
    }

    [Fact]
    public void FullBiddingLifecycle_HoldThenRelease_ResultsInOriginalBalance()
    {
        // Simulasi skenario: top up -> bid (hold) -> kalah/tersalip (release)
        var wallet = Wallet.CreateFor(Guid.NewGuid());
        wallet.TopUp(5_000_000);

        wallet.Hold(1_000_000);
        wallet.Release(1_000_000); // tersalip bidder lain

        Assert.Equal(5_000_000, wallet.Balance); // balance kembali utuh
        Assert.Equal(0, wallet.HeldBalance);
        Assert.Equal(5_000_000, wallet.AvailableBalance);
    }
}