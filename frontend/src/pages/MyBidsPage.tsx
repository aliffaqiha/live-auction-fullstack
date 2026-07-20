import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyBidHistory, type MyBidHistoryItem } from "../services/apiClient";
import { Badge } from "../components/ui/badge";
import { Card, CardContent } from "../components/ui/card";

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
    <div className="mx-auto max-w-[800px] px-6">
      <span className="mb-2 block font-mono text-xs uppercase tracking-[0.1em] text-brass">
        Riwayat Saya
      </span>
      <h1 className="mb-6 font-display text-[28px] font-semibold">Lelang yang Saya Ikuti</h1>

      {loading ? (
        <p className="py-8 text-center text-muted-foreground">Memuat…</p>
      ) : items.length === 0 ? (
        <p className="py-8 text-center text-muted-foreground">Anda belum pernah mengajukan penawaran.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {items.map((item) => (
            <Link key={item.auctionId} to={`/auctions/${item.auctionId}`} className="no-underline">
              <Card className="transition-colors hover:border-border-strong">
                <CardContent className="flex items-center gap-4 p-3">
                  <div className="h-14 w-14 flex-shrink-0 overflow-hidden rounded-sm">
                    {item.thumbnailUrl ? (
                      <img src={item.thumbnailUrl} alt={item.itemTitle} className="h-full w-full object-cover" />
                    ) : (
                      <div className="h-full w-full bg-charcoal" />
                    )}
                  </div>
                  <div className="min-w-0 flex-1">
                    <h3 className="font-display text-[15px] font-semibold">{item.itemTitle}</h3>
                    <span className="text-xs text-muted-foreground">
                      Penawaran terakhir Anda: {formatIDR(item.myLastBidAmount)}
                    </span>
                  </div>
                  <div className="flex-shrink-0">
                    {item.isWinner && <Badge variant="sage">Menang</Badge>}
                    {!item.isWinner && item.auctionStatus === "Sold" && <Badge variant="ember">Kalah</Badge>}
                    {item.auctionStatus === "Active" && <Badge variant="brass">Berlangsung</Badge>}
                    {item.auctionStatus === "Unsold" && <Badge variant="muted">Gagal Terjual</Badge>}
                  </div>
                </CardContent>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
