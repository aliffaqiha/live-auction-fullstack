import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyBidHistory, type MyBidHistoryItem } from "../services/apiClient";
import "./HistoryPages.css";

function formatIDR(amount: number) {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function MyBidsPage() {
  const [items, setItems] = useState<MyBidHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyBidHistory()
      .then(setItems)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="history-page">
      <span className="history-page__eyebrow">Riwayat Saya</span>
      <h1 className="history-page__title">Lelang yang Saya Ikuti</h1>

      {loading ? (
        <p className="history-page__empty">Memuat…</p>
      ) : items.length === 0 ? (
        <p className="history-page__empty">Anda belum pernah mengajukan penawaran.</p>
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
                  Penawaran terakhir Anda: {formatIDR(item.myLastBidAmount)}
                </span>
              </div>
              <div className="history-row__status">
                {item.isWinner && <span className="history-badge history-badge--win">Menang</span>}
                {!item.isWinner && item.auctionStatus === "Sold" && (
                  <span className="history-badge history-badge--lose">Kalah</span>
                )}
                {item.auctionStatus === "Active" && (
                  <span className="history-badge history-badge--active">Berlangsung</span>
                )}
                {item.auctionStatus === "Unsold" && (
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