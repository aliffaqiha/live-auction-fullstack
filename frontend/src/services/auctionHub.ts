import * as signalR from "@microsoft/signalr";

const HUB_URL = import.meta.env.VITE_API_BASE_URL
  ? `${import.meta.env.VITE_API_BASE_URL}/hubs/auction`
  : "http://localhost:8080/hubs/auction";

let connection: signalR.HubConnection | null = null;
let startPromise: Promise<void> | null = null;

export function getAuctionConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();
  }
  return connection;
}

async function ensureConnected(): Promise<void> {
  const conn = getAuctionConnection();
  if (conn.state === signalR.HubConnectionState.Connected) return;

  if (!startPromise) {
    startPromise = conn.start().catch((err) => {
      startPromise = null;
      throw err;
    });
  }
  await startPromise;
}

export async function joinAuctionRoom(auctionId: string) {
  try {
    await ensureConnected();
    const conn = getAuctionConnection();
    if (conn.state === signalR.HubConnectionState.Connected) {
      await conn.invoke("JoinAuctionRoom", auctionId);
    }
  } catch (err) {
    console.warn("Gagal join auction room:", err);
  }
}

export async function leaveAuctionRoom(auctionId: string) {
  try {
    const conn = getAuctionConnection();
    if (conn.state === signalR.HubConnectionState.Connected) {
      await conn.invoke("LeaveAuctionRoom", auctionId);
    }
  } catch (err) {
    console.warn("Gagal leave auction room:", err);
  }
}