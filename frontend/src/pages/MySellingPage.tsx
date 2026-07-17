import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMySellingHistory, type MySellingHistoryItem } from "../services/apiClient";
import "./HistoryPages.css";

function formatIDR(amount: number) {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function MySellingPage() {
  const [items, setItems] = useState<MySellingHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMySellingHistory()
      .then(setItems)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="history-page">
      <span className="history-page__eyebrow">Panel Penjual</span>
      <h1 className="history-page__title">Riwayat Penjualan</h1>

      {loading ? (
        <p className="history-page__empty">Memuat…</p>
      ) : items.length === 0 ? (
        <p className="history-page__empty">Belum ada lelang yang selesai.</p>
      ) : (
        <div className="history-list">
          {items.map((item) => (
            <Link key={item.auctionId} to={`/auctions/${item.auctionId}`} className="history-row">
              <div className="history-row__image">
                {item.thumbnailUrl ? (
                  <img src={item.thumbnailUrl} alt={item.itemTitle} />
                ) : (
                  <div className="history-row__placeholder" />
                )}
              </div>
              <div className="history-row__info">
                <h3>{item.itemTitle}</h3>
                <span className="history-row__meta">
                  Harga awal: {formatIDR(item.startingPrice)} · {item.totalBids} penawaran
                  {item.finalPrice && ` · Terjual: ${formatIDR(item.finalPrice)}`}
                </span>
              </div>
              <div className="history-row__status">
                {item.outcome === "Sold" ? (
                  <span className="history-badge history-badge--win">Terjual</span>
                ) : (
                  <span className="history-badge history-badge--unsold">Gagal Terjual</span>
                )}
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}