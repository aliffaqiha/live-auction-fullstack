import { useEffect, useState } from "react";
import { useParams, Link } from "react-router-dom";
import { apiClient, cancelAuction } from "../services/apiClient";
import { Button } from "../components/ui/button";
import { Card, CardContent } from "../components/ui/card";
import { Alert, AlertDescription } from "../components/ui/alert";

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

  if (loading) return <div className="mx-auto max-w-[800px] px-6">Memuat...</div>;

  return (
    <div className="mx-auto max-w-[800px] px-6">
      <div className="mb-6 flex items-end justify-between">
        <div>
          <span className="mb-2 block font-mono text-xs uppercase tracking-[0.1em] text-brass">
            Panel Penjual
          </span>
          <h1 className="font-display text-[28px] font-semibold">Riwayat Lelang Item</h1>
        </div>
        <Button asChild variant="outline">
          <Link to="/my-items">← Kembali</Link>
        </Button>
      </div>

      {error && (
        <Alert variant="destructive" className="mb-4">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {auctions.length === 0 ? (
        <p className="py-8 text-center text-muted-foreground">Belum ada lelang untuk item ini.</p>
      ) : (
        <div className="flex flex-col gap-3">
          {auctions.map((auction) => (
            <Card key={auction.id}>
              <CardContent className="flex items-center gap-4 p-3">
                <div className="min-w-0 flex-1">
                  <h3 className="font-display text-base font-semibold">
                    Lelang #{auction.id.slice(0, 8).toUpperCase()}
                  </h3>
                  <span className="text-xs text-muted-foreground">
                    Status: {auction.status} ·{" "}
                    {auction.currentHighestBid
                      ? `Bid tertinggi: ${formatIDR(auction.currentHighestBid)}`
                      : `Harga awal: ${formatIDR(auction.startingPrice)}`}
                  </span>
                </div>
                <div className="flex gap-2">
                  <Button asChild variant="outline" size="sm">
                    <Link to={`/auctions/${auction.id}`}>Lihat</Link>
                  </Button>
                  {(auction.status === "Active" || auction.status === "Scheduled") && (
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleCancel(auction.id)}
                      disabled={cancelling === auction.id}
                    >
                      {cancelling === auction.id ? "Membatalkan..." : "Batalkan"}
                    </Button>
                  )}
                  {auction.status === "Unsold" && (
                    <Button asChild size="sm">
                      <Link to={`/my-items/${itemId}/create-auction`}>Relist</Link>
                    </Button>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
