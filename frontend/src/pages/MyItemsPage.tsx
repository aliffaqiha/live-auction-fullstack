import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { getMyItems, type MyItem } from "../services/apiClient";
import { Button } from "../components/ui/button";
import { Card, CardContent } from "../components/ui/card";

export function MyItemsPage() {
  const [items, setItems] = useState<MyItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyItems()
      .then(setItems)
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="mx-auto max-w-[800px] px-6">
      <div className="mb-6 flex items-end justify-between">
        <div>
          <span className="mb-2 block font-mono text-xs uppercase tracking-[0.1em] text-brass">
            Panel Penjual
          </span>
          <h1 className="font-display text-[28px] font-semibold">Item Saya</h1>
        </div>
        <Button asChild>
          <Link to="/my-items/new">+ Tambah Item</Link>
        </Button>
      </div>

      {loading ? (
        <p className="py-8 text-center text-muted-foreground">Memuat…</p>
      ) : items.length === 0 ? (
        <div className="flex flex-col items-center gap-4 py-8 text-center text-muted-foreground">
          <p>Anda belum memiliki item apapun.</p>
          <Button asChild>
            <Link to="/my-items/new">Tambah item pertama Anda</Link>
          </Button>
        </div>
      ) : (
        <div className="flex flex-col gap-3">
          {items.map((item) => (
            <Card key={item.id}>
              <CardContent className="flex items-center gap-4 p-3">
                <div className="h-16 w-16 flex-shrink-0 overflow-hidden rounded-sm">
                  {item.thumbnailUrl ? (
                    <img src={item.thumbnailUrl} alt={item.title} className="h-full w-full object-cover" />
                  ) : (
                    <div className="h-full w-full bg-charcoal" />
                  )}
                </div>
                <div className="min-w-0 flex-1">
                  <h3 className="font-display text-base font-semibold">{item.title}</h3>
                  <span className="text-xs text-muted-foreground">
                    {item.categoryName} · Kondisi: {item.condition}
                  </span>
                </div>
                <div className="flex gap-2">
                  <Button asChild variant="outline" size="sm">
                    <Link to={`/my-items/${item.id}/auctions`}>Kelola Lelang</Link>
                  </Button>
                  {!item.hasActiveAuction && (
                    <Button asChild size="sm">
                      <Link to={`/my-items/${item.id}/create-auction`}>Buat Lelang</Link>
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
