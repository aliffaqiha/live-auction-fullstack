import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyItems, type MyItem } from "../services/apiClient";
import "./MyItemsPage.css";

export function MyItemsPage() {
  const [items, setItems] = useState<MyItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyItems()
      .then(setItems)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="my-items-page">
      <div className="my-items-page__header">
        <div>
          <span className="my-items-page__eyebrow">Panel Penjual</span>
          <h1 className="my-items-page__title">Item Saya</h1>
        </div>
        <Link to="/my-items/new" className="my-items-page__add-btn">
          + Tambah Item
        </Link>
      </div>

      {loading ? (
        <p className="my-items-page__empty">Memuat…</p>
      ) : items.length === 0 ? (
        <div className="my-items-page__empty-state">
          <p>Anda belum memiliki item apapun.</p>
          <Link to="/my-items/new" className="my-items-page__add-btn">
            Tambah item pertama Anda
          </Link>
        </div>
      ) : (
        <div className="my-items-list">
          {items.map((item) => (
            <div key={item.id} className="my-items-row">
              <div className="my-items-row__image">
                {item.thumbnailUrl ? (
                  <img src={item.thumbnailUrl} alt={item.title} />
                ) : (
                  <div className="my-items-row__placeholder" />
                )}
              </div>
              <div className="my-items-row__info">
                <h3>{item.title}</h3>
                <span className="my-items-row__meta">
                  {item.categoryName} · Kondisi: {item.condition}
                </span>
              </div>
              <div className="my-items-row__action">
                <Link
                  to={`/my-items/${item.id}/auctions`}
                  className="my-items-row__auction-btn"
                  style={{ marginRight: "8px" }}
                >
                  Kelola Lelang
                </Link>
                {!item.hasActiveAuction && (
                  <Link to={`/my-items/${item.id}/create-auction`} className="my-items-row__auction-btn">
                    Buat Lelang
                  </Link>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}