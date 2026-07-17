import {type FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./AuthPages.css";

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState("Bidder");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await register(email, password, fullName, role);
      navigate("/");
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Pendaftaran gagal, coba lagi.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <span className="auth-card__eyebrow">Ruang Lelang</span>
        <h1 className="auth-card__title">Daftarkan paddle baru</h1>
        <p className="auth-card__subtitle">Bergabung sebagai peserta lelang atau penjual.</p>

        <form onSubmit={handleSubmit} className="auth-form">
          <label className="auth-form__field">
            <span>Nama lengkap</span>
            <input value={fullName} onChange={(e) => setFullName(e.target.value)} required placeholder="Nama Anda" />
          </label>

          <label className="auth-form__field">
            <span>Email</span>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
              placeholder="nama@email.com"
            />
          </label>

          <label className="auth-form__field">
            <span>Kata sandi</span>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={6}
              placeholder="Minimal 6 karakter"
            />
          </label>

          <label className="auth-form__field">
            <span>Daftar sebagai</span>
            <select value={role} onChange={(e) => setRole(e.target.value)}>
              <option value="Bidder">Peserta lelang (Bidder)</option>
              <option value="Seller">Penjual (Seller)</option>
            </select>
          </label>

          {error && <p className="auth-form__error">{error}</p>}

          <button type="submit" className="auth-form__submit" disabled={loading}>
            {loading ? "Memproses…" : "Daftar"}
          </button>
        </form>

        <p className="auth-card__footer">
          Sudah punya paddle? <Link to="/login">Masuk di sini</Link>
        </p>
      </div>
    </div>
  );
}