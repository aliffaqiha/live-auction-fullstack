import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "./Navbar.css";

export function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const isSeller = user?.role === "Seller" || user?.role === "Both";

  async function handleLogout() {
    await logout();
    navigate("/");
  }

  return (
    <header className="navbar">
      <Link to="/" className="navbar__brand">
        <span className="navbar__brand-mark">⚒</span>
        <span className="navbar__brand-text">Balai Lelang</span>
      </Link>

      <nav className="navbar__actions">
        {user ? (
          <>
            {isSeller && (
              <Link to="/my-items" className="navbar__wallet-link">
                Item Saya
              </Link>
            )}
            {isSeller && (
              <Link to="/my-selling" className="navbar__wallet-link">
                Penjualan
              </Link>
            )}
            <Link to="/my-bids" className="navbar__wallet-link">
              Riwayat Bid
            </Link>
            <Link to="/wallet" className="navbar__wallet-link">
              Dompet
            </Link>
            <span className="navbar__paddle" title={user.email}>
              Paddle #{user.userId.slice(0, 4).toUpperCase()}
              {isSeller && " · Penjual"}
            </span>
            <button className="navbar__logout" onClick={handleLogout}>
              Keluar
            </button>
          </>
        ) : (
          <>
            <Link to="/login" className="navbar__link">
              Masuk
            </Link>
            <Link to="/register" className="navbar__cta">
              Daftar
            </Link>
          </>
        )}
      </nav>
    </header>
  );
}