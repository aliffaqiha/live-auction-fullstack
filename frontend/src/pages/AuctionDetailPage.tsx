import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { buyNowAuction, type AuctionDetail, type PriceHistoryEntry } from "../services/apiClient";
import { getAuctionDetail, placeBid, getItemPriceHistory } from "../services/apiClient";
import { getAuctionConnection, joinAuctionRoom, leaveAuctionRoom } from "../services/auctionHub";
import { useAuth } from "../context/AuthContext";
import { useCountdown } from "../hooks/useCountdown";
import "./AuctionDetailPage.css";

function formatIDR(amount: number) {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(amount);
}

interface NewBidEvent {
  auctionId: string;
  amount: number;
  bidderId: string;
  endTime: string;
}

export function AuctionDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { user } = useAuth();
  const [auction, setAuction] = useState<AuctionDetail | null>(null);
  const [priceHistory, setPriceHistory] = useState<PriceHistoryEntry[]>([]);
  const [activeImage, setActiveImage] = useState(0);
  const [bidAmount, setBidAmount] = useState<number>(0);
  const [bidError, setBidError] = useState<string | null>(null);
  const [bidSuccess, setBidSuccess] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const { label, isUrgent, isEnded } = useCountdown(auction?.endTime ?? new Date().toISOString());
  const [buyNowLoading, setBuyNowLoading] = useState(false);

  useEffect(() => {
    if (!id) return;

    getAuctionDetail(id).then((data) => {
      setAuction(data);
      const minNext = (data.currentHighestBid ?? data.startingPrice) + data.bidIncrement;
      setBidAmount(minNext);

      // Riwayat harga hanya relevan kalau item ini pernah di-relist sebelumnya
      if (data.relistCount > 0) {
        getItemPriceHistory(data.itemId).then(setPriceHistory);
      }
    });

    joinAuctionRoom(id);
    const connection = getAuctionConnection();

    connection.on("NewBid", (payload: NewBidEvent) => {
      if (payload.auctionId !== id) return;
      setAuction((prev) =>
        prev
          ? {
              ...prev,
              currentHighestBid: payload.amount,
              currentHighestBidderId: payload.bidderId,
              endTime: payload.endTime,
            }
          : prev
      );
      setBidAmount((prevAmount) => Math.max(prevAmount, payload.amount + (auction?.bidIncrement ?? 0)));
    });

    connection.on("Outbid", (payload: { auctionId: string; outbidUserId: string }) => {
      if (payload.auctionId === id && payload.outbidUserId === user?.userId) {
        setBidError("Anda baru saja tersalip! Ajukan penawaran baru.");
      }
    });

    return () => {
      leaveAuctionRoom(id);
      connection.off("NewBid");
      connection.off("Outbid");
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  async function handlePlaceBid() {
    if (!id) return;
    setBidError(null);
    setBidSuccess(false);
    setSubmitting(true);
    try {
      await placeBid(id, bidAmount);
      setBidSuccess(true);
    } catch (err: any) {
      setBidError(err?.response?.data?.error ?? "Gagal mengajukan penawaran.");
    } finally {
      setSubmitting(false);
    }
  }
  async function handleBuyNow() {
  if (!id) return;
  if (!confirm(`Beli langsung seharga ${formatIDR(auction!.buyNowPrice!)}?`)) return;
  setBuyNowLoading(true);
  setBidError(null);
  try {
    await buyNowAuction(id);
    setBidSuccess(true);
  } catch (err: any) {
    setBidError(err?.response?.data?.error ?? "Gagal membeli langsung.");
  } finally {
    setBuyNowLoading(false);
  }
}

  if (!auction) {
    return <div className="auction-detail-loading">Memuat lot…</div>;
  }

  const currentPrice = auction.currentHighestBid ?? auction.startingPrice;
  const isActive = auction.status === "Active" && !isEnded;
  const isOwnHighestBid = auction.currentHighestBidderId === user?.userId;

  return (
    <div className="auction-detail-page">
      <div className="auction-detail-gallery">
        <div className="auction-detail-gallery__main">
          {auction.imageUrls[activeImage] ? (
            <img src={auction.imageUrls[activeImage]} alt={auction.itemTitle} />
          ) : (
            <div className="auction-detail-gallery__placeholder" />
          )}
        </div>
        {auction.imageUrls.length > 1 && (
          <div className="auction-detail-gallery__thumbs">
            {auction.imageUrls.map((url, idx) => (
              <button
                key={url + idx}
                className={`auction-detail-gallery__thumb ${idx === activeImage ? "auction-detail-gallery__thumb--active" : ""}`}
                onClick={() => setActiveImage(idx)}
              >
                <img src={url} alt="" />
              </button>
            ))}
          </div>
        )}
      </div>

      <div className="auction-detail-info">
        <span className="auction-detail-info__category">{auction.categoryName}</span>
        <h1 className="auction-detail-info__title">{auction.itemTitle}</h1>
        <p className="auction-detail-info__meta">
          Kondisi: {auction.itemCondition} · Dijual oleh {auction.sellerName}
          {auction.relistCount > 0 && ` · Dilelang ulang (ke-${auction.relistCount + 1})`}
        </p>

        <p className="auction-detail-info__description">{auction.itemDescription}</p>

        <div className="auction-detail-panel">
          <div className="auction-detail-panel__price-block">
            <span className="auction-detail-panel__label">
              {auction.currentHighestBid ? "Penawaran tertinggi" : "Harga pembukaan"}
            </span>
            <span className="auction-detail-panel__price">{formatIDR(currentPrice)}</span>
            {isOwnHighestBid && <span className="auction-detail-panel__you-lead">Anda memimpin lelang ini</span>}
          </div>

          <div className={`auction-detail-panel__countdown ${isUrgent ? "auction-detail-panel__countdown--urgent" : ""}`}>
            <span className="auction-detail-panel__countdown-label">
              {auction.status === "Scheduled" ? "Dimulai dalam" : isEnded ? "Status" : "Waktu tersisa"}
            </span>
            <span className="auction-detail-panel__countdown-value">
              {auction.status === "Scheduled" ? "Belum dimulai" : isEnded ? "Palu telah jatuh" : label}
            </span>
          </div>

          {isActive && (
            <div className="auction-detail-bid-form">
              {!user ? (
                <p className="auction-detail-bid-form__notice">
                  Masuk terlebih dahulu untuk mengajukan penawaran.
                </p>
              ) : (
                <>
                  <label className="auction-detail-bid-form__field">
                    <span>
                      Penawaran Anda (min. {formatIDR(currentPrice + auction.bidIncrement)})
                    </span>
                    <input
                      type="number"
                      value={bidAmount}
                      min={currentPrice + auction.bidIncrement}
                      step={auction.bidIncrement}
                      onChange={(e) => setBidAmount(Number(e.target.value))}
                    />
                  </label>

                  {bidError && <p className="auction-detail-bid-form__error">{bidError}</p>}
                  {bidSuccess && (
                    <p className="auction-detail-bid-form__success">Penawaran Anda berhasil diajukan!</p>
                  )}

                  <button className="auction-detail-bid-form__submit" onClick={handlePlaceBid} disabled={submitting}>
                    {submitting ? "Mengajukan…" : "Ajukan Penawaran"}
                  </button>
                  {auction.buyNowPrice && (
                  <button
                    onClick={handleBuyNow}
                    disabled={buyNowLoading}
                        style={{
                                background: "var(--color-ember)",
                                color: "white",
                                border: "none",
                                padding: "12px",
                                borderRadius: "4px",
                                fontWeight: 600,
                                marginTop: "8px",
                              }}
                  >
                    {buyNowLoading ? "Memproses…" : `Beli Sekarang — ${formatIDR(auction.buyNowPrice)}`}
                  </button>
                )}
                </>
                
              )}
            </div>
          )}
        </div>

        {priceHistory.length > 0 && (
          <div className="auction-detail-price-history">
            <h2 className="auction-detail-history__title">Riwayat Lelang Barang Ini</h2>
            <p className="auction-detail-price-history__note">
              Barang ini sudah {priceHistory.length}x pernah dilelang sebelumnya.
            </p>
            <ul className="auction-detail-price-history__list">
              {priceHistory.map((entry) => (
                <li key={entry.auctionId} className="auction-detail-price-history__item">
                  <span className="auction-detail-price-history__attempt">Percobaan #{entry.relistAttempt}</span>
                  <span className="auction-detail-price-history__price">
                    {entry.outcome === "Sold" && entry.finalPrice
                      ? `Terjual ${formatIDR(entry.finalPrice)}`
                      : "Gagal terjual"}
                  </span>
                  <span className="auction-detail-price-history__date">
                    {new Date(entry.settledAt).toLocaleDateString("id-ID", { day: "numeric", month: "short", year: "numeric" })}
                  </span>
                </li>
              ))}
            </ul>
          </div>
        )}

        <div className="auction-detail-history">
          <h2 className="auction-detail-history__title">Riwayat Penawaran</h2>
          {auction.recentBids.length === 0 ? (
            <p className="auction-detail-history__empty">Belum ada penawaran masuk. Jadilah yang pertama.</p>
          ) : (
            <ul className="auction-detail-history__list">
              {auction.recentBids.map((bid) => (
                <li key={bid.bidId} className="auction-detail-history__item">
                  <span className="auction-detail-history__bidder">{bid.bidderName}</span>
                  <span className="auction-detail-history__amount">{formatIDR(bid.amount)}</span>
                  <span className="auction-detail-history__time">
                    {new Date(bid.placedAt).toLocaleTimeString("id-ID", { hour: "2-digit", minute: "2-digit" })}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
}