import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { apiClient } from "../services/apiClient";

interface AuthUser {
  userId: string;
  email: string;
  fullName: string;
  role: string;
}

interface AuthContextValue {
  user: AuthUser | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, fullName: string, role: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [loading, setLoading] = useState(true);

  async function refreshUser() {
    try {
      const { data } = await apiClient.get("/api/auth/me");
      setUser(data);
    } catch {
      setUser(null);
    }
  }

  useEffect(() => {
    refreshUser().finally(() => setLoading(false));
  }, []);

  async function login(email: string, password: string) {
  await apiClient.post("/api/auth/login", { email, password });
  await refreshUser();
}

  async function register(email: string, password: string, fullName: string, role: string) {
    await apiClient.post("/api/auth/register", { email, password, fullName, role });
    await login(email, password);
  }

  async function logout() {
    await apiClient.post("/api/auth/logout");
    setUser(null);
  }

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout, refreshUser }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth harus dipakai di dalam AuthProvider");
  return ctx;
}