import { useEffect, useState } from "react";
import type { AuctionSummary } from "../services/apiClient";
import { getAuctions } from "../services/apiClient";
import { LotCard } from "../components/LotCard";
import { Input } from "../components/ui/input";
import { Button } from "../components/ui/button";

const STATUS_FILTERS = [
  { value: "", label: "Semua" },
  { value: "Active", label: "Berlangsung" },
  { value: "Scheduled", label: "Segera dimulai" },
];

export function AuctionListPage() {
  const [auctions, setAuctions] = useState<AuctionSummary[]>([]);
  const [status, setStatus] = useState("");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    const timeout = setTimeout(() => {
      getAuctions({ status: status || undefined, search: search || undefined, pageSize: 24 })
        .then((res) => setAuctions(res.items))
        .finally(() => setLoading(false));
    }, 300);

    return () => clearTimeout(timeout);
  }, [status, search]);

  return (
    <div className="mx-auto max-w-[1200px] px-6">
      <section className="border-b border-border py-8 pb-6">
        <span className="mb-3 block font-mono text-xs uppercase tracking-[0.1em] text-brass">
          Katalog Malam Ini
        </span>
        <h1 className="mb-4 max-w-[700px] font-display text-4xl font-semibold leading-tight md:text-5xl">
          Setiap lot punya satu kesempatan.
        </h1>
        <p className="max-w-[560px] text-base leading-relaxed text-muted-foreground">
          Telusuri barang yang sedang dan akan dilelang. Palu jatuh saat waktu habis — tidak ada perpanjangan
          kecuali penawaran masuk di detik terakhir.
        </p>
      </section>

      <div className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div className="flex gap-2">
          {STATUS_FILTERS.map((f) => (
            <Button
              key={f.value}
              variant={status === f.value ? "default" : "outline"}
              size="sm"
              className="rounded-full"
              onClick={() => setStatus(f.value)}
            >
              {f.label}
            </Button>
          ))}
        </div>
        <Input
          type="search"
          className="w-[240px]"
          placeholder="Cari barang…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {loading ? (
        <div className="py-8 text-center text-muted-foreground">Memuat katalog…</div>
      ) : auctions.length === 0 ? (
        <div className="py-8 text-center text-muted-foreground">Tidak ada lot yang cocok dengan pencarian ini.</div>
      ) : (
        <div className="grid grid-cols-1 gap-5 pb-8 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
          {auctions.map((auction, idx) => (
            <LotCard key={auction.id} auction={auction} lotNumber={idx + 1} />
          ))}
        </div>
      )}
    </div>
  );
}
