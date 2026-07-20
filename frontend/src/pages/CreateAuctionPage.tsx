import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { createAuction, getItemPriceHistory, type PriceHistoryEntry } from "../services/apiClient";
import { Card, CardContent } from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Button } from "../components/ui/button";
import { Label } from "../components/ui/label";
import { Alert, AlertDescription } from "../components/ui/alert";

function toLocalInputValue(date: Date) {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

function formatIDR(amount: number) {
  return new Intl.NumberFormat("id-ID", {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0,
  }).format(amount);
}

export function CreateAuctionPage() {
  const { itemId } = useParams<{ itemId: string }>();
  const navigate = useNavigate();

  const inOneHour = new Date(Date.now() + 60 * 60 * 1000);
  const inThreeDays = new Date(Date.now() + 3 * 24 * 60 * 60 * 1000);

  const [startingPrice, setStartingPrice] = useState(100_000);
  const [bidIncrement, setBidIncrement] = useState(25_000);
  const [reservePrice, setReservePrice] = useState<number | "">("");
  const [startTime, setStartTime] = useState(toLocalInputValue(inOneHour));
  const [endTime, setEndTime] = useState(toLocalInputValue(inThreeDays));
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [priceHistory, setPriceHistory] = useState<PriceHistoryEntry[]>([]);
  const [buyNowPrice, setBuyNowPrice] = useState<number | "">("");

  useEffect(() => {
    if (!itemId) return;
    getItemPriceHistory(itemId).then(setPriceHistory).catch(() => setPriceHistory([]));
  }, [itemId]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!itemId) return;
    setError(null);
    setLoading(true);
    try {
      const result = await createAuction({
        itemId,
        startingPrice,
        reservePrice: reservePrice === "" ? null : reservePrice,
        bidIncrement,
        startTime: new Date(startTime).toISOString(),
        endTime: new Date(endTime).toISOString(),
        buyNowPrice: buyNowPrice === "" ? null : buyNowPrice,
      });
      navigate(`/auctions/${result.auctionId}`);
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Gagal membuat lelang.");
    } finally {
      setLoading(false);
    }
  }

  const isRelist = priceHistory.length > 0;

  return (
    <div className="mx-auto max-w-[560px] px-6">
      <span className="mb-2 block font-mono text-xs uppercase tracking-[0.1em] text-brass">
        Panel Penjual
      </span>
      <h1 className="mb-2 font-display text-[28px] font-semibold">
        {isRelist ? "Lelang Ulang (Relist)" : "Buat Lelang"}
      </h1>
      <p className="mb-6 text-sm leading-relaxed text-muted-foreground">
        {isRelist
          ? `Ini akan menjadi percobaan lelang ke-${priceHistory.length + 1} untuk barang ini.`
          : "Tentukan harga pembukaan, kelipatan penawaran, dan jadwal lelang untuk item ini."}
      </p>

      {isRelist && (
        <Card className="mb-5">
          <CardContent className="p-4">
            <h3 className="mb-2 text-sm text-muted-foreground">Riwayat lelang sebelumnya:</h3>
            <ul className="flex flex-col">
              {priceHistory.map((entry) => (
                <li
                  key={entry.auctionId}
                  className="flex items-center justify-between border-b border-border py-1.5 text-[13px] last:border-b-0"
                >
                  <span>Percobaan #{entry.relistAttempt}</span>
                  <span>
                    {entry.outcome === "Sold" && entry.finalPrice
                      ? `Terjual ${formatIDR(entry.finalPrice)}`
                      : entry.outcome === "Cancelled"
                      ? "Dibatalkan"
                      : "Gagal terjual"}
                  </span>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">Harga Pembukaan (IDR)</Label>
              <Input
                type="number"
                value={startingPrice}
                onChange={(e) => setStartingPrice(Number(e.target.value))}
                min={1}
                required
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">Kelipatan Penawaran (IDR)</Label>
              <Input
                type="number"
                value={bidIncrement}
                onChange={(e) => setBidIncrement(Number(e.target.value))}
                min={1}
                required
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">
                Harga Cadangan / Reserve Price (opsional)
              </Label>
              <Input
                type="number"
                value={reservePrice}
                onChange={(e) => setReservePrice(e.target.value === "" ? "" : Number(e.target.value))}
                placeholder="Kosongkan jika tidak ada"
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">
                Harga Beli Sekarang / Buy Now (opsional)
              </Label>
              <Input
                type="number"
                value={buyNowPrice}
                onChange={(e) => setBuyNowPrice(e.target.value === "" ? "" : Number(e.target.value))}
                placeholder="Kosongkan jika tidak ada opsi beli langsung"
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">Waktu Mulai</Label>
              <Input
                type="datetime-local"
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
                required
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">Waktu Berakhir</Label>
              <Input
                type="datetime-local"
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
                required
              />
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <Button type="submit" className="mt-2 w-full" disabled={loading}>
              {loading ? "Membuat lelang…" : isRelist ? "Lelang Ulang Sekarang" : "Buat Lelang"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
