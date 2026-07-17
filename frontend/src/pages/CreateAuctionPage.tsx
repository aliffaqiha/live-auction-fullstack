import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createAuction, getItemPriceHistory, type PriceHistoryEntry } from "../services/apiClient";
import "./ItemForm.css";

function toLocalInputValue(date: Date) {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function formatIDR(amount: number) {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function CreateAuctionPage() {
  const { itemId } = useParams<{ itemId: string }>();
  const navigate = useNavigate();

  const inOneHour = new Date(Date.now() + 60 * 60 * 1000);
  const inThreeDays = new Date(Date.now() + 3 * 24 * 60 * 60 * 1000);

  const [startingPrice, setStartingPrice] = useState(100_000);
  const [bidIncrement, setBidIncrement] = useState(25_000);
  const [reservePrice, setReservePrice] = useState<number | "">("");
  const [startTime, setStartTime] = useState(toLocalInputValue(inOneHour));
  const [endTime, setEndTime] = useState(toLocalInputValue(inThreeDays));
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [priceHistory, setPriceHistory] = useState<PriceHistoryEntry[]>([]);
  const [buyNowPrice, setBuyNowPrice] = useState<number | "">("");

  useEffect(() => {
    if (!itemId) return;
    getItemPriceHistory(itemId).then(setPriceHistory).catch(() => setPriceHistory([]));
  }, [itemId]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!itemId) return;
    setError(null);
    setLoading(true);
    try {
      const result = await createAuction({
        itemId,
        startingPrice,
        reservePrice: reservePrice === "" ? null : reservePrice,
        bidIncrement,
        startTime: new Date(startTime).toISOString(),
        endTime: new Date(endTime).toISOString(),
        buyNowPrice: buyNowPrice === "" ? null : buyNowPrice,
      });
      navigate(`/auctions/${result.auctionId}`);
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Gagal membuat lelang.");
    } finally {
      setLoading(false);
    }
  }

  const isRelist = priceHistory.length > 0;

  return (
    <div className="item-form-page">
      <span className="item-form-page__eyebrow">Panel Penjual</span>
      <h1 className="item-form-page__title">{isRelist ? "Lelang Ulang (Relist)" : "Buat Lelang"}</h1>
      <p className="item-form-page__subtitle">
        {isRelist
          ? `Ini akan menjadi percobaan lelang ke-${priceHistory.length + 1} untuk barang ini.`
          : "Tentukan harga pembukaan, kelipatan penawaran, dan jadwal lelang untuk item ini."}
      </p>

      {isRelist && (
        <div
          style={{
            background: "var(--color-surface)",
            border: "1px solid var(--color-border)",
            borderRadius: "8px",
            padding: "16px",
            marginBottom: "20px",
          }}
        >
          <h3 style={{ fontSize: "14px", margin: "0 0 8px", color: "var(--color-ink-muted)" }}>
            Riwayat lelang sebelumnya:
          </h3>
          <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
            {priceHistory.map((entry) => (
              <li
                key={entry.auctionId}
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  fontSize: "13px",
                  padding: "6px 0",
                  borderBottom: "1px solid var(--color-border)",
                }}
              >
                <span>Percobaan #{entry.relistAttempt}</span>
                <span>
                  {entry.outcome === "Sold" && entry.finalPrice
                    ? `Terjual ${formatIDR(entry.finalPrice)}`
                    : entry.outcome === "Cancelled"
                    ? "Dibatalkan"
                    : "Gagal terjual"}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <form onSubmit={handleSubmit} className="item-form">
        <label className="item-form__field">
          <span>Harga Pembukaan (IDR)</span>
          <input
            type="number"
            value={startingPrice}
            onChange={(e) => setStartingPrice(Number(e.target.value))}
            min={1}
            required
          />
        </label>

        <label className="item-form__field">
          <span>Kelipatan Penawaran (IDR)</span>
          <input
            type="number"
            value={bidIncrement}
            onChange={(e) => setBidIncrement(Number(e.target.value))}
            min={1}
            required
          />
        </label>

        <label className="item-form__field">
          <span>Harga Cadangan / Reserve Price (opsional)</span>
          <input
            type="number"
            value={reservePrice}
            onChange={(e) => setReservePrice(e.target.value === "" ? "" : Number(e.target.value))}
            placeholder="Kosongkan jika tidak ada"
          />
        </label>

        <label className="item-form__field">
          <span>Harga Beli Sekarang / Buy Now (opsional)</span>
          <input
            type="number"
            value={buyNowPrice}
            onChange={(e) => setBuyNowPrice(e.target.value === "" ? "" : Number(e.target.value))}
            placeholder="Kosongkan jika tidak ada opsi beli langsung"
          />
        </label>

        <label className="item-form__field">
          <span>Waktu Mulai</span>
          <input type="datetime-local" value={startTime} onChange={(e) => setStartTime(e.target.value)} required />
        </label>

        <label className="item-form__field">
          <span>Waktu Berakhir</span>
          <input type="datetime-local" value={endTime} onChange={(e) => setEndTime(e.target.value)} required />
        </label>

        {error && <p className="item-form__error">{error}</p>}

        <button type="submit" className="item-form__submit" disabled={loading}>
          {loading ? "Membuat lelang…" : isRelist ? "Lelang Ulang Sekarang" : "Buat Lelang"}
        </button>
      </form>
    </div>
  );
}