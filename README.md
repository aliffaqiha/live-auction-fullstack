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

Balai Lelang adalah simulasi platform lelang online (mirip eBay/Christie's digital) di mana:

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