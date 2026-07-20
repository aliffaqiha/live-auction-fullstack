import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { type Category, createItem, getCategories } from "../services/apiClient";
import { Card, CardContent } from "../components/ui/card";
import { Input } from "../components/ui/input";
import { Button } from "../components/ui/button";
import { Label } from "../components/ui/label";
import { Textarea } from "../components/ui/textarea";
import { Alert, AlertDescription } from "../components/ui/alert";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "../components/ui/select";

const CONDITIONS = ["Baru", "Seperti Baru", "Used", "Perlu Perbaikan"];

export function CreateItemPage() {
  const navigate = useNavigate();
  const [categories, setCategories] = useState<Category[]>([]);
  const [categoryId, setCategoryId] = useState("");
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [condition, setCondition] = useState(CONDITIONS[0]);
  const [imageUrl, setImageUrl] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    getCategories().then((cats) => {
      setCategories(cats);
      if (cats.length > 0) setCategoryId(cats[0].id);
    });
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const result = await createItem({
        categoryId,
        title,
        description,
        condition,
        imageUrls: imageUrl ? [imageUrl] : [`https://picsum.photos/seed/${Date.now()}/400/300`],
      });
      navigate(`/my-items/${result.itemId}/create-auction`);
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Gagal menambahkan item.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="mx-auto max-w-[560px] px-6">
      <span className="mb-2 block font-mono text-xs uppercase tracking-[0.1em] text-brass">
        Panel Penjual
      </span>
      <h1 className="mb-2 font-display text-[28px] font-semibold">Tambah Item Baru</h1>
      <p className="mb-6 text-sm leading-relaxed text-muted-foreground">
        Daftarkan barang yang ingin Anda lelang. Setelah item tersimpan, Anda bisa langsung membuat lelangnya.
      </p>

      <Card>
        <CardContent className="p-6">
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">Kategori</Label>
              <Select value={categoryId} onValueChange={setCategoryId}>
                <SelectTrigger>
                  <SelectValue placeholder="Pilih kategori" />
                </SelectTrigger>
                <SelectContent>
                  {categories.map((c) => (
                    <SelectItem key={c.id} value={c.id}>
                      {c.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">Judul Item</Label>
              <Input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                required
                placeholder="Cth: Jam Tangan Vintage"
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">Deskripsi</Label>
              <Textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                required
                rows={4}
                placeholder="Jelaskan kondisi, riwayat, dan detail barang Anda…"
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">Kondisi</Label>
              <Select value={condition} onValueChange={setCondition}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CONDITIONS.map((c) => (
                    <SelectItem key={c} value={c}>
                      {c}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col gap-2">
              <Label className="text-[13px] text-muted-foreground">
                URL Gambar (opsional — kosongkan untuk gambar acak)
              </Label>
              <Input
                value={imageUrl}
                onChange={(e) => setImageUrl(e.target.value)}
                placeholder="https://…"
              />
            </div>

            {error && (
              <Alert variant="destructive">
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <Button type="submit" className="mt-2 w-full" disabled={loading}>
              {loading ? "Menyimpan…" : "Simpan & Lanjutkan"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
