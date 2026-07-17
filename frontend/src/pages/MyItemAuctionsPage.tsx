import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { apiClient, cancelAuction } from "../services/apiClient";
import "./MyItemsPage.css";

interface AuctionInfo {
  id: string;
  status: string;
  startingPrice: number;
  currentHighestBid: number | null;
  startTime: string;
  endTime: string;
}

function formatIDR(amount: number) {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function MyItemAuctionsPage() {
  const { itemId } = useParams<{ itemId: string }>();
  const [auctions, setAuctions] = useState<AuctionInfo[]>([]);
  const [loading, setLoading] = useState(true);
  const [cancelling, setCancelling] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function loadAuctions() {
    const { data } = await apiClient.get(`/api/items/${itemId}/auctions`);
    setAuctions(data);
    setLoading(false);
  }

  useEffect(() => {
    loadAuctions();
  }, [itemId]);

  async function handleCancel(auctionId: string) {
    if (!confirm("Yakin ingin membatalkan lelang ini? Semua saldo bidder yang tertahan akan dikembalikan.")) return;
    setCancelling(auctionId);
    setError(null);
    try {
      await cancelAuction(auctionId);
      await loadAuctions();
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Gagal membatalkan lelang.");
    } finally {
      setCancelling(null);
    }
  }

  if (loading) return <div className="my-items-page">Memuat...</div>;

  return (
    <div className="my-items-page">
      <div className="my-items-page__header">
        <div>
          <span className="my-items-page__eyebrow">Panel Penjual</span>
          <h1 className="my-items-page__title">Riwayat Lelang Item</h1>
        </div>
        <Link to="/my-items" className="my-items-page__add-btn">
          ← Kembali
        </Link>
      </div>

      {error && (
        <p style={{ color: "var(--color-ember)", marginBottom: "16px" }}>{error}</p>
      )}

      {auctions.length === 0 ? (
        <p className="my-items-page__empty">Belum ada lelang untuk item ini.</p>
      ) : (
        <div className="my-items-list">
          {auctions.map((auction) => (
            <div key={auction.id} className="my-items-row">
              <div className="my-items-row__info">
                <h3>
                  Lelang #{auction.id.slice(0, 8).toUpperCase()}
                </h3>
                <span className="my-items-row__meta">
                  Status: {auction.status} ·{" "}
                  {auction.currentHighestBid
                    ? `Bid tertinggi: ${formatIDR(auction.currentHighestBid)}`
                    : `Harga awal: ${formatIDR(auction.startingPrice)}`}
                </span>
              </div>
              <div className="my-items-row__action">
                <Link
                  to={`/auctions/${auction.id}`}
                  className="my-items-row__auction-btn"
                  style={{ marginRight: "8px" }}
                >
                  Lihat
                </Link>
                {(auction.status === "Active" || auction.status === "Scheduled") && (
                  <button
                    onClick={() => handleCancel(auction.id)}
                    disabled={cancelling === auction.id}
                    style={{
                      background: "none",
                      border: "1px solid var(--color-ember)",
                      color: "var(--color-ember)",
                      padding: "6px 14px",
                      borderRadius: "4px",
                      cursor: "pointer",
                      fontSize: "13px",
                      fontWeight: 600,
                    }}
                  >
                    {cancelling === auction.id ? "Membatalkan..." : "Batalkan"}
                  </button>
                )}
                {auction.status === "Unsold" && (
                  <Link
                    to={`/my-items/${itemId}/create-auction`}
                    className="my-items-row__auction-btn"
                  >
                    Relist
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