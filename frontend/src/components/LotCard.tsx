import { Link } from "react-router-dom";
import type { AuctionSummary } from "../services/apiClient";
import { useCountdown } from "../hooks/useCountdown";
import "./LotCard.css";

function formatIDR(amount: number) {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function LotCard({ auction, lotNumber }: { auction: AuctionSummary; lotNumber: number }) {
  const { label, isUrgent, isEnded } = useCountdown(auction.endTime);
  const currentPrice = auction.currentHighestBid ?? auction.startingPrice;
  const isScheduled = auction.status === "Scheduled";

  return (
    <Link to={`/auctions/${auction.id}`} className="lot-card">
      <div className="lot-card__image-wrap">
        {auction.thumbnailUrl ? (
          <img src={auction.thumbnailUrl} alt={auction.itemTitle} className="lot-card__image" />
        ) : (
          <div className="lot-card__image-placeholder" />
        )}
        <span className="lot-card__lot-tag">LOT {String(lotNumber).padStart(3, "0")}</span>
        {isScheduled && <span className="lot-card__status-tag lot-card__status-tag--scheduled">Segera</span>}
      </div>

      <div className="lot-card__body">
        <span className="lot-card__category">{auction.categoryName}</span>
        <h3 className="lot-card__title">{auction.itemTitle}</h3>

        <div className="lot-card__price-row">
          <div>
            <span className="lot-card__price-label">
              {auction.currentHighestBid ? "Penawaran tertinggi" : "Harga awal"}
            </span>
            <span className="lot-card__price">{formatIDR(currentPrice)}</span>
          </div>

          {!isScheduled && (
            <div className={`lot-card__countdown ${isUrgent ? "lot-card__countdown--urgent" : ""}`}>
              {isEnded ? "Selesai" : label}
            </div>
          )}
        </div>

        <div className="lot-card__meta">
          <span>{auction.totalBids} penawaran</span>
        </div>
      </div>
    </Link>
  );
}