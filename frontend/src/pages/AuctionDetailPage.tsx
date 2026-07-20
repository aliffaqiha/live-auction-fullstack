import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { buyNowAuction, type AuctionDetail, type PriceHistoryEntry } from "../services/apiClient";
import { getAuctionDetail, placeBid, getItemPriceHistory } from "../services/apiClient";
import { getAuctionConnection, joinAuctionRoom, leaveAuctionRoom } from "../services/auctionHub";
import { useAuth } from "../context/AuthContext";
import { useCountdown } from "../hooks/useCountdown";
import { Card, CardContent } from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Button } from "../components/ui/button";
import { Label } from "../components/ui/label";
import { Alert, AlertDescription } from "../components/ui/alert";
import { Separator } from "../components/ui/separator";

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
    return <div className="p-8 text-center text-muted-foreground">Memuat lot…</div>;
  }

  const currentPrice = auction.currentHighestBid ?? auction.startingPrice;
  const isActive = auction.status === "Active" && !isEnded;
  const isOwnHighestBid = auction.currentHighestBidderId === user?.userId;

  return (
    <div className="mx-auto grid max-w-[1100px] gap-7 p-6 md:grid-cols-2">
      {/* Gallery */}
      <div>
        <div className="aspect-[4/3] overflow-hidden rounded-lg bg-card">
          {auction.imageUrls[activeImage] ? (
            <img
              src={auction.imageUrls[activeImage]}
              alt={auction.itemTitle}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="h-full w-full bg-gradient-to-br from-card to-charcoal" />
          )}
        </div>
        {auction.imageUrls.length > 1 && (
          <div className="mt-3 flex gap-2">
            {auction.imageUrls.map((url, idx) => (
              <button
                key={url + idx}
                className={`h-16 w-16 overflow-hidden rounded-sm border-2 bg-transparent p-0 ${
                  idx === activeImage ? "border-brass" : "border-transparent"
                }`}
                onClick={() => setActiveImage(idx)}
              >
                <img src={url} alt="" className="h-full w-full object-cover" />
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Info */}
      <div>
        <span className="text-[11px] uppercase tracking-[0.08em] text-muted-foreground">
          {auction.categoryName}
        </span>
        <h1 className="my-2 font-display text-[32px] font-semibold leading-tight">{auction.itemTitle}</h1>
        <p className="mb-4 text-[13px] text-muted-foreground">
          Kondisi: {auction.itemCondition} · Dijual oleh {auction.sellerName}
          {auction.relistCount > 0 && ` · Dilelang ulang (ke-${auction.relistCount + 1})`}
        </p>
        <p className="mb-6 text-sm leading-relaxed text-foreground">{auction.itemDescription}</p>

        {/* Bid Panel */}
        <Card className="mb-6">
          <CardContent className="p-5">
            <div className="mb-4 flex flex-col">
              <span className="mb-1 text-xs text-muted-foreground">
                {auction.currentHighestBid ? "Penawaran tertinggi" : "Harga pembukaan"}
              </span>
              <span className="font-mono text-[30px] font-semibold text-brass">{formatIDR(currentPrice)}</span>
              {isOwnHighestBid && (
                <span className="mt-2 text-xs font-semibold text-sage">Anda memimpin lelang ini</span>
              )}
            </div>

            <Separator className="mb-4" />

            <div className="mb-4 flex items-center justify-between">
              <span className="text-[13px] text-muted-foreground">
                {auction.status === "Scheduled" ? "Dimulai dalam" : isEnded ? "Status" : "Waktu tersisa"}
              </span>
              <span className={`font-mono text-base font-semibold ${isUrgent ? "text-ember" : ""}`}>
                {auction.status === "Scheduled"
                  ? "Belum dimulai"
                  : isEnded
                  ? "Palu telah jatuh"
                  : label}
              </span>
            </div>

            {isActive && (
              <div className="flex flex-col gap-3 border-t border-border pt-4">
                {!user ? (
                  <p className="text-[13px] text-muted-foreground">
                    Masuk terlebih dahulu untuk mengajukan penawaran.
                  </p>
                ) : (
                  <>
                    <div className="flex flex-col gap-2">
                      <Label className="text-[13px] text-muted-foreground">
                        Penawaran Anda (min. {formatIDR(currentPrice + auction.bidIncrement)})
                      </Label>
                      <Input
                        type="number"
                        className="font-mono text-base"
                        value={bidAmount}
                        min={currentPrice + auction.bidIncrement}
                        step={auction.bidIncrement}
                        onChange={(e) => setBidAmount(Number(e.target.value))}
                      />
                    </div>

                    {bidError && (
                      <Alert variant="destructive">
                        <AlertDescription>{bidError}</AlertDescription>
                      </Alert>
                    )}
                    {bidSuccess && (
                      <Alert className="border-sage/50 bg-sage/15 text-sage">
                        <AlertDescription>Penawaran Anda berhasil diajukan!</AlertDescription>
                      </Alert>
                    )}

                    <Button className="w-full" onClick={handlePlaceBid} disabled={submitting}>
                      {submitting ? "Mengajukan…" : "Ajukan Penawaran"}
                    </Button>
                    {auction.buyNowPrice && (
                      <Button
                        variant="destructive"
                        className="w-full"
                        onClick={handleBuyNow}
                        disabled={buyNowLoading}
                      >
                        {buyNowLoading
                          ? "Memproses…"
                          : `Beli Sekarang — ${formatIDR(auction.buyNowPrice)}`}
                      </Button>
                    )}
                  </>
                )}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Price History (relist) */}
        {priceHistory.length > 0 && (
          <Card className="mb-6">
            <CardContent className="p-5">
              <h2 className="mb-1 font-display text-lg font-semibold">Riwayat Lelang Barang Ini</h2>
              <p className="mb-4 text-[13px] text-muted-foreground">
                Barang ini sudah {priceHistory.length}x pernah dilelang sebelumnya.
              </p>
              <ul className="flex flex-col">
                {priceHistory.map((entry) => (
                  <li
                    key={entry.auctionId}
                    className="flex items-center justify-between border-b border-border py-3 text-[13px] last:border-b-0"
                  >
                    <span className="font-mono text-[12px] text-muted-foreground">
                      Percobaan #{entry.relistAttempt}
                    </span>
                    <span className="font-semibold">
                      {entry.outcome === "Sold" && entry.finalPrice
                        ? `Terjual ${formatIDR(entry.finalPrice)}`
                        : "Gagal terjual"}
                    </span>
                    <span className="text-[12px] text-muted-foreground">
                      {new Date(entry.settledAt).toLocaleDateString("id-ID", {
                        day: "numeric",
                        month: "short",
                        year: "numeric",
                      })}
                    </span>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        )}

        {/* Bid History */}
        <Card>
          <CardContent className="p-5">
            <h2 className="mb-3 font-display text-lg font-semibold">Riwayat Penawaran</h2>
            {auction.recentBids.length === 0 ? (
              <p className="text-[13px] text-muted-foreground">
                Belum ada penawaran masuk. Jadilah yang pertama.
              </p>
            ) : (
              <ul className="flex flex-col">
                {auction.recentBids.map((bid) => (
                  <li
                    key={bid.bidId}
                    className="flex items-center justify-between border-b border-border py-3 text-[13px] last:border-b-0"
                  >
                    <span className="text-foreground">{bid.bidderName}</span>
                    <span className="font-mono text-brass">{formatIDR(bid.amount)}</span>
                    <span className="text-[12px] text-muted-foreground">
                      {new Date(bid.placedAt).toLocaleTimeString("id-ID", {
                        hour: "2-digit",
                        minute: "2-digit",
                      })}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
