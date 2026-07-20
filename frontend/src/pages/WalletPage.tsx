import { useEffect, useState } from "react";
import { getWallet, topUpWallet, type WalletInfo } from "../services/apiClient";
import { Card, CardContent, CardHeader, CardTitle } from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Button } from "../components/ui/button";

function formatIDR(amount: number) {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(amount);
}

const QUICK_AMOUNTS = [500_000, 1_000_000, 5_000_000, 10_000_000];

export function WalletPage() {
  const [wallet, setWallet] = useState<WalletInfo | null>(null);
  const [amount, setAmount] = useState(1_000_000);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

  function loadWallet() {
    getWallet().then(setWallet);
  }

  useEffect(() => {
    loadWallet();
  }, []);

  async function handleTopUp() {
    setLoading(true);
    setMessage(null);
    try {
      await topUpWallet(amount);
      setMessage("Saldo berhasil ditambahkan.");
      loadWallet();
    } catch (err: any) {
      setMessage(err?.response?.data?.error ?? "Top up gagal.");
    } finally {
      setLoading(false);
    }
  }

  if (!wallet) return <div className="p-8 text-center text-muted-foreground">Memuat dompet…</div>;

  return (
    <div className="mx-auto max-w-[640px] px-6">
      <span className="mb-2 block font-mono text-xs uppercase tracking-[0.1em] text-brass">
        Dompet Anda
      </span>
      <h1 className="mb-6 font-display text-[30px] font-semibold">Saldo & Riwayat</h1>

      <div className="mb-6 grid grid-cols-3 gap-3">
        <Card>
          <CardContent className="flex flex-col gap-2 p-4">
            <span className="text-xs text-muted-foreground">Saldo total</span>
            <strong className="font-mono text-lg">{formatIDR(wallet.balance)}</strong>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex flex-col gap-2 p-4">
            <span className="text-xs text-muted-foreground">Sedang ditahan (bid aktif)</span>
            <strong className="font-mono text-lg text-ember">{formatIDR(wallet.heldBalance)}</strong>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="flex flex-col gap-2 p-4">
            <span className="text-xs text-muted-foreground">Tersedia untuk bid</span>
            <strong className="font-mono text-lg text-sage">{formatIDR(wallet.availableBalance)}</strong>
          </CardContent>
        </Card>
      </div>

      <Card className="mb-5">
        <CardContent className="p-5">
          <CardHeader className="p-0">
            <CardTitle className="font-display text-[17px] font-semibold">Tambah saldo</CardTitle>
          </CardHeader>
          <div className="mt-4 flex flex-wrap gap-2">
            {QUICK_AMOUNTS.map((amt) => (
              <Button
                key={amt}
                variant={amount === amt ? "outline" : "ghost"}
                size="sm"
                className={`rounded-sm font-mono text-[13px] ${
                  amount === amt ? "border-brass text-brass" : ""
                }`}
                onClick={() => setAmount(amt)}
              >
                {formatIDR(amt)}
              </Button>
            ))}
          </div>
          <div className="mt-4 flex gap-3">
            <Input
              type="number"
              className="flex-1 font-mono"
              value={amount}
              onChange={(e) => setAmount(Number(e.target.value))}
              min={1}
            />
            <Button onClick={handleTopUp} disabled={loading}>
              {loading ? "Memproses…" : "Top Up"}
            </Button>
          </div>
          {message && (
            <p className="mt-3 text-[13px] text-sage">{message}</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardContent className="p-5">
          <CardHeader className="p-0">
            <CardTitle className="font-display text-[17px] font-semibold">Transaksi terakhir</CardTitle>
          </CardHeader>
          {wallet.recentTransactions.length === 0 ? (
            <p className="mt-4 text-[13px] text-muted-foreground">Belum ada transaksi.</p>
          ) : (
            <ul className="mt-4 flex flex-col">
              {wallet.recentTransactions.map((t) => (
                <li
                  key={t.id}
                  className="flex items-center justify-between border-b border-border py-3 text-[13px] last:border-b-0"
                >
                  <span className="font-semibold">{t.type}</span>
                  <span className="font-mono text-brass">{formatIDR(t.amount)}</span>
                  <span className="text-[12px] text-muted-foreground">
                    {new Date(t.createdAt).toLocaleString("id-ID")}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
