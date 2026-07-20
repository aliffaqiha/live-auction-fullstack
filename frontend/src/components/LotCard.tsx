import { Link } from "react-router-dom";
import type { AuctionSummary } from "../services/apiClient";
import { useCountdown } from "../hooks/useCountdown";
import { Badge } from "./ui/badge";

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
    <Link
      to={`/auctions/${auction.id}`}
      className="group flex flex-col rounded-lg border border-border bg-card text-card-foreground transition-all hover:-translate-y-1 hover:border-border-strong"
    >
      <div className="relative aspect-[4/3] overflow-hidden rounded-t-lg bg-charcoal">
        {auction.thumbnailUrl ? (
          <img src={auction.thumbnailUrl} alt={auction.itemTitle} className="h-full w-full object-cover" />
        ) : (
          <div className="h-full w-full bg-gradient-to-br from-card to-charcoal" />
        )}
        <span className="absolute left-3 top-3 rounded-sm border border-brass/35 bg-background/85 px-2 py-0.5 font-mono text-[11px] font-medium tracking-wider text-brass">
          LOT {String(lotNumber).padStart(3, "0")}
        </span>
        {isScheduled && (
          <Badge variant="sage" className="absolute right-3 top-3">
            Segera
          </Badge>
        )}
      </div>

      <div className="flex flex-col gap-2 p-4">
        <span className="text-[11px] uppercase tracking-[0.08em] text-muted-foreground">
          {auction.categoryName}
        </span>
        <h3 className="font-display text-lg font-semibold leading-snug text-foreground">{auction.itemTitle}</h3>

        <div className="mt-2 flex items-end justify-between">
          <div>
            <span className="mb-0.5 block text-[11px] text-muted-foreground">
              {auction.currentHighestBid ? "Penawaran tertinggi" : "Harga awal"}
            </span>
            <span className="font-mono text-[17px] font-semibold text-brass">{formatIDR(currentPrice)}</span>
          </div>

          {!isScheduled && (
            <span
              className={`rounded-sm border px-2 py-0.5 font-mono text-[13px] whitespace-nowrap ${
                isUrgent
                  ? "animate-pulse border-ember text-ember"
                  : "border-border-strong text-muted-foreground"
              }`}
            >
              {isEnded ? "Selesai" : label}
            </span>
          )}
        </div>

        <span className="mt-1 text-xs text-muted-foreground">{auction.totalBids} penawaran</span>
      </div>
    </Link>
  );
}
