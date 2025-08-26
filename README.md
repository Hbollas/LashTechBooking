# LashTechBooking — Single-Tech Salon Booking (ASP.NET Core MVC)

A production-style booking site for a lash technician. Clients book without accounts; the owner approves in an admin dashboard. Built to showcase ASP.NET Core, EF Core, Identity, and clean layering.

## Features
- **Public booking (no user accounts):** pick a service/date/time with server-side validation and phone/email checks.
- **One-tech double-booking guard:** prevents overlaps across all services; time slots are generated in real time.
- **Admin dashboard:** filter by date range/status, **Approve/Cancel** bookings, toggle **Deposit Paid**, and **Export CSV**.
- **Email notifications:** approval/cancellation emails via `IEmailSender` (SMTP or console fallback).
- **Polished UI:** Tailwind/brand.css styling, responsive layout, and a before/after gallery.
- **Security:** Admin-only area (Identity + roles), antiforgery, HTTPS redirection, validated inputs.
- **Data model:** EF Core with SQLite; prices stored safely (decimal with converter), migrations included and seeded demo data.

## Tech Stack
- **ASP.NET Core 9** MVC, **Entity Framework Core** (SQLite)
- **ASP.NET Core Identity** (admin role only)
- **Tailwind CSS** + custom `brand.css`
- Project layout: `Domain`, `Application`, `Infrastructure`, `Web`

## Run locally
```bash
# from repo root
dotnet restore

# create/update DB
dotnet ef database update --project Infrastructure --startup-project Web

# run
dotnet run --project Web
