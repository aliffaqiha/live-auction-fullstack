import { useEffect, useState } from "react";
import { getWallet, topUpWallet, type WalletInfo } from "../services/apiClient";
import "./WalletPage.css";

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

  if (!wallet) return <div className="wallet-page-loading">Memuat dompet…</div>;

  return (
    <div className="wallet-page">
      <span className="wallet-page__eyebrow">Dompet Anda</span>
      <h1 className="wallet-page__title">Saldo & Riwayat</h1>

      <div className="wallet-summary">
        <div className="wallet-summary__item">
          <span>Saldo total</span>
          <strong>{formatIDR(wallet.balance)}</strong>
        </div>
        <div className="wallet-summary__item">
          <span>Sedang ditahan (bid aktif)</span>
          <strong className="wallet-summary__held">{formatIDR(wallet.heldBalance)}</strong>
        </div>
        <div className="wallet-summary__item">
          <span>Tersedia untuk bid</span>
          <strong className="wallet-summary__available">{formatIDR(wallet.availableBalance)}</strong>
        </div>
      </div>

      <div className="wallet-topup">
        <h2>Tambah saldo</h2>
        <div className="wallet-topup__quick">
          {QUICK_AMOUNTS.map((amt) => (
            <button
              key={amt}
              className={`wallet-topup__quick-btn ${amount === amt ? "wallet-topup__quick-btn--active" : ""}`}
              onClick={() => setAmount(amt)}
            >
              {formatIDR(amt)}
            </button>
          ))}
        </div>
        <div className="wallet-topup__custom">
          <input
            type="number"
            value={amount}
            onChange={(e) => setAmount(Number(e.target.value))}
            min={1}
          />
          <button onClick={handleTopUp} disabled={loading}>
            {loading ? "Memproses…" : "Top Up"}
          </button>
        </div>
        {message && <p className="wallet-topup__message">{message}</p>}
      </div>

      <div className="wallet-history">
        <h2>Transaksi terakhir</h2>
        {wallet.recentTransactions.length === 0 ? (
          <p className="wallet-history__empty">Belum ada transaksi.</p>
        ) : (
          <ul className="wallet-history__list">
            {wallet.recentTransactions.map((t) => (
              <li key={t.id} className="wallet-history__item">
                <span className="wallet-history__type">{t.type}</span>
                <span className="wallet-history__amount">{formatIDR(t.amount)}</span>
                <span className="wallet-history__date">
                  {new Date(t.createdAt).toLocaleString("id-ID")}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}