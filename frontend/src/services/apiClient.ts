import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "http://localhost:8080",
  headers: { "Content-Type": "application/json" },
  withCredentials: true,
});

// ── Auctions ──────────────────────────────────────────────────────────────

export interface AuctionSummary {
  id: string;
  itemId: string;
  itemTitle: string;
  thumbnailUrl: string | null;
  categoryName: string;
  startingPrice: number;
  currentHighestBid: number | null;
  bidIncrement: number;
  buyNowPrice: number | null;
  startTime: string;
  endTime: string;
  status: string;
  totalBids: number;
}

export interface AuctionListResponse {
  items: AuctionSummary[];
  total: number;
  page: number;
  pageSize: number;
}

export interface BidHistoryItem {
  bidId: string;
  bidderName: string;
  amount: number;
  placedAt: string;
  status: string;
}

export interface AuctionDetail {
  id: string;
  itemId: string;
  itemTitle: string;
  itemDescription: string;
  itemCondition: string;
  imageUrls: string[];
  categoryName: string;
  sellerName: string;
  startingPrice: number;
  reservePrice: number | null;
  bidIncrement: number;
  buyNowPrice: number | null;
  currentHighestBid: number | null;
  currentHighestBidderId: string | null;
  startTime: string;
  endTime: string;
  status: string;
  relistCount: number;
  recentBids: BidHistoryItem[];
}

export async function getAuctions(params: {
  status?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}): Promise<AuctionListResponse> {
  const { data } = await apiClient.get("/api/auctions", { params });
  return data;
}

export async function getAuctionDetail(id: string): Promise<AuctionDetail> {
  const { data } = await apiClient.get(`/api/auctions/${id}`);
  return data;
}

export async function placeBid(auctionId: string, amount: number) {
  const { data } = await apiClient.post(`/api/auctions/${auctionId}/bids`, { amount });
  return data;
}

// ── Wallet ────────────────────────────────────────────────────────────────

export interface WalletInfo {
  balance: number;
  heldBalance: number;
  availableBalance: number;
  recentTransactions: {
    id: string;
    type: string;
    amount: number;
    referenceId: string | null;
    createdAt: string;
  }[];
}

export async function getWallet(): Promise<WalletInfo> {
  const { data } = await apiClient.get("/api/wallet");
  return data;
}

export async function topUpWallet(amount: number) {
  const { data } = await apiClient.post("/api/wallet/topup", { amount });
  return data;
}

// ── Items & Categories ────────────────────────────────────────────────────

export interface Category {
  id: string;
  name: string;
  slug: string;
}

export interface MyItem {
  id: string;
  title: string;
  categoryName: string;
  condition: string;
  thumbnailUrl: string | null;
  hasActiveAuction: boolean;
  createdAt: string;
}

export async function getCategories(): Promise<Category[]> {
  const { data } = await apiClient.get("/api/categories");
  return data;
}

export async function getMyItems(): Promise<MyItem[]> {
  const { data } = await apiClient.get("/api/items/mine");
  return data;
}

export async function createItem(payload: {
  categoryId: string;
  title: string;
  description: string;
  condition: string;
  imageUrls: string[];
}) {
  const { data } = await apiClient.post("/api/items", payload);
  return data;
}

export async function createAuction(payload: {
  itemId: string;
  startingPrice: number;
  reservePrice: number | null;
  bidIncrement: number;
  buyNowPrice: number | null;
  startTime: string;
  endTime: string;
}) {
  const { data } = await apiClient.post("/api/auctions", payload);
  return data;
}

// ── Riwayat ───────────────────────────────────────────────────────────────

export interface MyBidHistoryItem {
  auctionId: string;
  itemTitle: string;
  thumbnailUrl: string | null;
  myLastBidAmount: number;
  finalPrice: number | null;
  auctionStatus: string;
  isWinner: boolean;
  endTime: string;
}

export interface MySellingHistoryItem {
  auctionId: string;
  itemId: string;
  itemTitle: string;
  thumbnailUrl: string | null;
  startingPrice: number;
  finalPrice: number | null;
  outcome: string;
  totalBids: number;
  endTime: string;
}

export interface PriceHistoryEntry {
  auctionId: string;
  startingPrice: number;
  finalPrice: number | null;
  outcome: string;
  settledAt: string;
  relistAttempt: number;
}


export async function buyNowAuction(auctionId: string) {
  const { data } = await apiClient.post(`/api/auctions/${auctionId}/buy-now`);
  return data;
}
export async function getMyBidHistory(): Promise<MyBidHistoryItem[]> {
  const { data } = await apiClient.get("/api/auctions/my-bids");
  return data;
}

export async function getMySellingHistory(): Promise<MySellingHistoryItem[]> {
  const { data } = await apiClient.get("/api/auctions/my-selling");
  return data;
}

export async function getItemPriceHistory(itemId: string): Promise<PriceHistoryEntry[]> {
  const { data } = await apiClient.get(`/api/items/${itemId}/price-history`);
  return data;
}

export async function cancelAuction(auctionId: string) {
  const { data } = await apiClient.delete(`/api/auctions/${auctionId}`);
  return data;
}
