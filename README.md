# Balai Lelang — Live Auction Platform

Platform lelang online real-time dengan sistem bidding yang aman dari race condition, dibangun sebagai portofolio full-stack dengan fokus pada **concurrency handling**, **clean architecture**, dan **real-time communication**.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)

---

## Daftar Isi

- [Ringkasan](#ringkasan)
- [Fitur](#fitur)
- [Arsitektur](#arsitektur)
- [Tech Stack](#tech-stack)
- [Concurrency Handling](#concurrency-handling)
- [Cara Menjalankan](#cara-menjalankan)
- [Struktur Project](#struktur-project)
- [Testing](#testing)
- [Akun Demo](#akun-demo)
- [Keterbatasan & Rencana Lanjutan](#keterbatasan--rencana-lanjutan)

---

## Ringkasan

- **Seller** bisa mendaftarkan barang, membuat lelang, membatalkan lelang, dan melakukan relist kalau barang gagal terjual
- **Bidder** bisa menawar barang secara real-time, membeli langsung lewat opsi Buy Now, dan melihat riwayat lelang yang pernah diikuti
- **Sistem** secara otomatis mengelola siklus hidup lelang — mengaktifkan lelang terjadwal, menutup lelang yang sudah berakhir, dan melakukan settlement (penentuan pemenang + potong saldo) tanpa campur tangan manual

Studi kasus utama project ini adalah menyelesaikan **race condition**: apa yang terjadi kalau dua orang menawar di detik yang sama pada barang yang sama? Solusinya diimplementasikan dengan kombinasi *pessimistic locking* (`SELECT FOR UPDATE`) dan *optimistic concurrency* (PostgreSQL `xmin`).

---

## Fitur

### Autentikasi & Otorisasi
- Register/Login dengan JWT disimpan di HttpOnly Cookie (aman dari XSS)
- Role-based access control (Bidder, Seller, Both)

### Lelang
- Buat, lihat, cari, dan filter lelang
- Bidding real-time via WebSocket (SignalR) — semua viewer melihat update harga tanpa refresh
- Anti-sniping: bid di detik-detik akhir otomatis memperpanjang waktu lelang
- Buy Now — beli langsung tanpa menunggu lelang berakhir
- Reserve price — harga minimum tersembunyi, lelang batal kalau tidak tercapai
- Cancel auction oleh seller (saldo bidder otomatis dikembalikan)
- Relist — barang gagal terjual bisa dilelang ulang (maks. 3x), lengkap dengan riwayat harga lintas percobaan

### Wallet
- Top-up saldo (simulasi)
- Hold otomatis saat bid diajukan, release saat tersalip, deduct saat menang
- Audit trail lengkap untuk semua transaksi

### Otomasi
- **Background job siklus lelang** — mengaktifkan lelang terjadwal & menutup lelang berakhir setiap 15 detik, termasuk settlement (penentuan pemenang, potong/kembalikan saldo)
- **Bot bidding simulasi** — akun bot menawar secara acak di interval tidak teratur, supaya demo terlihat "hidup" tanpa perlu banyak user asli
- **Notifikasi email** (simulasi via console log) — menang lelang, tersalip, lelang berakhir

### Riwayat & Transparansi
- Riwayat bid yang pernah diikuti (menang/kalah)
- Riwayat penjualan untuk seller
- Riwayat harga barang lintas beberapa kali lelang (relist)

---

## Arsitektur

Backend menggunakan **Onion Architecture**, dengan aturan dependency yang selalu mengarah ke dalam:
API  →  Infrastructure  →  Application  →  Domain

- **Domain** — entity dengan business logic murni (`Auction.AcceptBid()`, `Wallet.Hold()`), tanpa dependency ke framework apapun. Bisa di-unit-test tanpa database.
- **Application** — use case (CQRS via MediatR), interface abstraksi (`IApplicationDbContext`), validasi (FluentValidation)
- **Infrastructure** — implementasi konkret: EF Core + PostgreSQL, SignalR Hub, background services, JWT service
- **API** — controllers, middleware, entry point

Pola ini dipilih supaya business rule (misalnya "seller tidak boleh bid di lelang sendiri", atau "bid harus lebih besar dari increment minimum") bisa diuji dan dipahami terpisah dari detail teknis seperti database atau HTTP.

---

## Tech Stack

**Backend**
- .NET 10, ASP.NET Core Web API
- Entity Framework Core + Npgsql (PostgreSQL provider)
- MediatR (CQRS)
- FluentValidation
- SignalR (WebSocket)
- JWT + BCrypt
- xUnit (unit testing)

**Frontend**
- React 18 + TypeScript
- Vite
- React Router
- Axios
- @microsoft/signalr (client)
- CSS custom (design token system, tanpa framework UI pihak ketiga)

**Database & Infrastruktur**
- PostgreSQL 16
- MinIO (object storage, disiapkan untuk upload gambar)
- Docker & Docker Compose

---

## Concurrency Handling

Ini bagian paling penting dari project ini. Dua strategi locking dipakai berdampingan:

**1. Pessimistic Locking**

Saat bid diajukan, row `Auction` dikunci lewat raw SQL:

```sql
SELECT *, xmin FROM "Auctions" WHERE "Id" = @auctionId FOR UPDATE
```

Request bid lain yang menyasar auction yang sama akan **menunggu** sampai transaksi pertama commit/rollback, sehingga tidak ada dua bid yang lolos validasi berdasarkan data yang sudah usang.

**2. Optimistic Concurrency**

Kolom sistem PostgreSQL `xmin` dipetakan sebagai *concurrency token* di EF Core (`AuctionConfiguration.cs`). Setiap `UPDATE` otomatis menyertakan `WHERE xmin = @originalXmin`; kalau row sudah berubah sejak terakhir dibaca, EF Core melempar `DbUpdateConcurrencyException`.

**Alur lengkap satu bid:**
Request masuk → Buka transaction → Lock row Auction (FOR UPDATE)
→ Validasi domain logic (Auction.AcceptBid)
→ Release hold bidder sebelumnya (kalau ada)
→ Hold saldo bidder baru
→ Simpan Bid + WalletTransaction
→ Commit transaction
→ Broadcast ke semua viewer via SignalR

---

## Cara Menjalankan

### Prasyarat
- Docker & Docker Compose
- (Opsional, untuk development lokal tanpa Docker) .NET 10 SDK, Node.js 20+

### Menjalankan dengan Docker

```bash
git clone <repo-url>
cd LiveAuction
docker compose up --build
```

Setelah semua container jalan:

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| Backend API | http://localhost:8080 |
| MinIO Console | http://localhost:9001 (minioadmin / minioadmin) |
| PostgreSQL | localhost:5432 (auction / auction123) |

Database akan otomatis ter-migrate dan ter-seed dengan data dummy (50 barang dari DummyJSON, 15+ user, 30 lelang) saat backend pertama kali start tidak perlu command tambahan.

### Development Lokal (tanpa Docker)

```bash
# Backend
cd backend
dotnet restore
dotnet run --project src/AuctionPlatform.API

# Frontend
cd frontend
npm install
npm run dev
```

---

## Struktur Project
LiveAuction/
├── docker-compose.yml
├── backend/
│   ├── AuctionPlatform.sln
│   ├── src/
│   │   ├── AuctionPlatform.Domain/          # Entity & business logic murni
│   │   ├── AuctionPlatform.Application/     # Use case (CQRS), validasi
│   │   ├── AuctionPlatform.Infrastructure/  # EF Core, SignalR, background jobs
│   │   └── AuctionPlatform.API/             # Controllers, entry point
│   └── tests/
│       └── AuctionPlatform.Domain.Tests/    # Unit test domain logic
└── frontend/
└── src/
├── components/                       # Navbar, LotCard, dll
├── pages/                             # Halaman-halaman aplikasi
├── context/                           # AuthContext
├── hooks/                             # useCountdown, dll
└── services/                          # apiClient, auctionHub (SignalR)

---

## Testing

```bash
cd backend
dotnet test tests/AuctionPlatform.Domain.Tests
```

32 unit test mencakup:
- Validasi pembuatan & pengajuan bid (harga invalid, di luar waktu aktif, di bawah increment)
- Anti-sniping (perpanjangan waktu otomatis)
- Settlement (Sold vs Unsold berdasarkan reserve price)
- Wallet (hold/release/deduct, saldo tidak cukup, skenario siklus lengkap)

---

## Akun Demo

Setelah seeding, akun berikut tersedia (password: `Password123!` untuk semua):

| Email | Role |
|---|---|
| seller1@auction.com – seller5@auction.com | Seller |
| bidder1@auction.com – bidder10@auction.com | Bidder (dipakai juga sebagai bot) |

Atau daftar akun baru sendiri lewat halaman Register.

---

## Keterbatasan & Rencana Lanjutan

- **Upload gambar** — saat ini pakai input URL manual, belum terhubung ke MinIO
- **Email notifikasi** — masih disimulasikan lewat console log, belum terhubung ke SMTP asli
- **Proxy/auto-bid** — field sudah tersedia di domain model, logic belum diimplementasikan
- **Testing** — baru mencakup unit test domain layer; integration test (API + database) dan frontend test belum ada
- **Rate limiting** — endpoint publik belum dibatasi dari spam request
- **Scaling** — SignalR belum pakai Redis backplane, sehingga hanya bekerja benar untuk single instance backend

---

## Lisensi

Project ini dibuat untuk keperluan portofolio pembelajaran.