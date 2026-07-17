import { useEffect, useState } from "react";

export function useCountdown(endTime: string) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(interval);
  }, []);

  const end = new Date(endTime).getTime();
  const diffMs = end - now;
  const isEnded = diffMs <= 0;
  const isUrgent = diffMs > 0 && diffMs < 60 * 60 * 1000; // < 1 jam

  if (isEnded) return { label: "Berakhir", isUrgent: false, isEnded: true };

  const totalSeconds = Math.floor(diffMs / 1000);
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);
  const seconds = totalSeconds % 60;

  let label: string;
  if (days > 0) label = `${days}h ${hours}j`;
  else if (hours > 0) label = `${hours}j ${minutes}m`;
  else label = `${minutes}m ${seconds.toString().padStart(2, "0")}d`;

  return { label, isUrgent, isEnded: false };
}