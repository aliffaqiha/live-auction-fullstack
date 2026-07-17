import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { type Category, createItem, getCategories } from "../services/apiClient";
import "./ItemForm.css";

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
    <div className="item-form-page">
      <span className="item-form-page__eyebrow">Panel Penjual</span>
      <h1 className="item-form-page__title">Tambah Item Baru</h1>
      <p className="item-form-page__subtitle">
        Daftarkan barang yang ingin Anda lelang. Setelah item tersimpan, Anda bisa langsung membuat lelangnya.
      </p>

      <form onSubmit={handleSubmit} className="item-form">
        <label className="item-form__field">
          <span>Kategori</span>
          <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} required>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>
        </label>

        <label className="item-form__field">
          <span>Judul Item</span>
          <input value={title} onChange={(e) => setTitle(e.target.value)} required placeholder="Cth: Jam Tangan Vintage" />
        </label>

        <label className="item-form__field">
          <span>Deskripsi</span>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            required
            rows={4}
            placeholder="Jelaskan kondisi, riwayat, dan detail barang Anda…"
          />
        </label>

        <label className="item-form__field">
          <span>Kondisi</span>
          <select value={condition} onChange={(e) => setCondition(e.target.value)}>
            {CONDITIONS.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </select>
        </label>

        <label className="item-form__field">
          <span>URL Gambar (opsional — kosongkan untuk gambar acak)</span>
          <input
            value={imageUrl}
            onChange={(e) => setImageUrl(e.target.value)}
            placeholder="https://…"
          />
        </label>

        {error && <p className="item-form__error">{error}</p>}

        <button type="submit" className="item-form__submit" disabled={loading}>
          {loading ? "Menyimpan…" : "Simpan & Lanjutkan"}
        </button>
      </form>
    </div>
  );
}