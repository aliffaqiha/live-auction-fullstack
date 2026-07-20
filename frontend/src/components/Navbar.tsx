import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { Button } from "./ui/button";

export function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const isSeller = user?.role === "Seller" || user?.role === "Both";

  async function handleLogout() {
    await logout();
    navigate("/");
  }

  return (
    <header className="sticky top-0 z-10 flex items-center justify-between border-b border-border bg-background px-6 py-4">
      <Link to="/" className="flex items-center gap-2 text-foreground no-underline">
        <span className="text-xl text-brass">⚒</span>
        <span className="font-display text-lg font-semibold tracking-tight">Balai Lelang</span>
      </Link>

      <nav className="flex items-center gap-4">
        {user ? (
          <>
            {isSeller && (
              <Link to="/my-items" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
                Item Saya
              </Link>
            )}
            {isSeller && (
              <Link to="/my-selling" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
                Penjualan
              </Link>
            )}
            <Link to="/my-bids" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
              Riwayat Bid
            </Link>
            <Link to="/wallet" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
              Dompet
            </Link>
            <span
              className="rounded-full border border-border-strong px-3 py-1 font-mono text-xs text-brass"
              title={user.email}
            >
              Paddle #{user.userId.slice(0, 4).toUpperCase()}
              {isSeller && " · Penjual"}
            </span>
            <Button variant="outline" size="sm" onClick={handleLogout}>
              Keluar
            </Button>
          </>
        ) : (
          <>
            <Link to="/login" className="text-sm text-muted-foreground transition-colors hover:text-foreground">
              Masuk
            </Link>
            <Button asChild size="sm">
              <Link to="/register">Daftar</Link>
            </Button>
          </>
        )}
      </nav>
    </header>
  );
}
