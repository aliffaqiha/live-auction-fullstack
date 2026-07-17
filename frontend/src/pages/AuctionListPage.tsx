import { useEffect, useState } from "react";
import type { AuctionSummary } from "../services/apiClient";
import { getAuctions } from "../services/apiClient";
import { LotCard } from "../components/LotCard";
import "./AuctionListPage.css";

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
    }, 300); // debounce search

    return () => clearTimeout(timeout);
  }, [status, search]);

  return (
    <div className="auction-list-page">
      <section className="auction-list-hero">
        <span className="auction-list-hero__eyebrow">Katalog Malam Ini</span>
        <h1 className="auction-list-hero__title">Setiap lot punya satu kesempatan.</h1>
        <p className="auction-list-hero__subtitle">
          Telusuri barang yang sedang dan akan dilelang. Palu jatuh saat waktu habis — tidak ada perpanjangan
          kecuali penawaran masuk di detik terakhir.
        </p>
      </section>

      <div className="auction-list-controls">
        <div className="auction-list-filters">
          {STATUS_FILTERS.map((f) => (
            <button
              key={f.value}
              className={`auction-list-filter ${status === f.value ? "auction-list-filter--active" : ""}`}
              onClick={() => setStatus(f.value)}
            >
              {f.label}
            </button>
          ))}
        </div>
        <input
          type="search"
          className="auction-list-search"
          placeholder="Cari barang…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {loading ? (
        <div className="auction-list-empty">Memuat katalog…</div>
      ) : auctions.length === 0 ? (
        <div className="auction-list-empty">Tidak ada lot yang cocok dengan pencarian ini.</div>
      ) : (
        <div className="auction-list-grid">
          {auctions.map((auction, idx) => (
            <LotCard key={auction.id} auction={auction} lotNumber={idx + 1} />
          ))}
        </div>
      )}
    </div>
  );
}