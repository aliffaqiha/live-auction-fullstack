import { BrowserRouter, Routes, Route } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { Navbar } from "./components/Navbar";
import { AuctionListPage } from "./pages/AuctionListPage";
import { AuctionDetailPage } from "./pages/AuctionDetailPage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { WalletPage } from "./pages/WalletPage";
import { MyItemsPage } from "./pages/MyItemsPage";
import { CreateItemPage } from "./pages/CreateItemPage";
import { CreateAuctionPage } from "./pages/CreateAuctionPage";
import { MyItemAuctionsPage } from "./pages/MyItemAuctionsPage";
import { MyBidsPage } from "./pages/MyBidsPage";
import { MySellingPage } from "./pages/MySellingPage";

function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Navbar />
        <Routes>
          <Route path="/" element={<AuctionListPage />} />
          <Route path="/auctions/:id" element={<AuctionDetailPage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/wallet" element={<WalletPage />} />
          <Route path="/my-items" element={<MyItemsPage />} />
          <Route path="/my-items/new" element={<CreateItemPage />} />
          <Route path="/my-items/:itemId/create-auction" element={<CreateAuctionPage />} />
          <Route path="/my-items/:itemId/auctions" element={<MyItemAuctionsPage />} />
          <Route path="/my-bids" element={<MyBidsPage />} />
          <Route path="/my-selling" element={<MySellingPage />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}

export default App;