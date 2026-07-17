import { type FormEvent, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./AuthPages.css";

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      await login(email, password);
      navigate("/");
    } catch (err: any) {
      setError(err?.response?.data?.error ?? "Email atau kata sandi salah.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-page">
      <div className="auth-card">
        <span className="auth-card__eyebrow">Ruang Lelang</span>
        <h1 className="auth-card__title">Masuk sebagai peserta</h1>
        <p className="auth-card__subtitle">Ambil paddle Anda dan mulai menawar.</p>

        <form onSubmit={handleSubmit} className="auth-form">
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
              placeholder="••••••••"
            />
          </label>

          {error && <p className="auth-form__error">{error}</p>}

          <button type="submit" className="auth-form__submit" disabled={loading}>
            {loading ? "Memproses…" : "Masuk"}
          </button>
        </form>

        <p className="auth-card__footer">
          Belum punya paddle? <Link to="/register">Daftar di sini</Link>
        </p>
      </div>
    </div>
  );
}