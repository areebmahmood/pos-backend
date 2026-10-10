# POS Backend (.NET 10 Web API)

REST API for a retail point-of-sale system: admin and salesman dashboards, multi-bill checkout, cash register
open/close, barcodes, discounts, returns, expenses, stock adjustments and daily/weekly/monthly/yearly sales reports.
Receipts carry Urdu instructions and a QR code.

The companion web app lives in [`pos-frontend`](https://github.com/areebmahmood/pos-frontend).

## Features

| # | Requirement | Where |
|---|---|---|
| 1, 2 | Admin and salesman dashboards | `GET /api/dashboard/admin`, `GET /api/dashboard/salesman` |
| 3 | Multiple independent billing windows | Stateless `POST /api/sales`; each bill is built client-side and submitted on checkout |
| 4, 16 | Cash in hand: open / close register | `/api/registers/open`, `/api/registers/{id}/close` (expected vs counted cash) |
| 5 | Barcode | Auto-generated EAN-13 on product create, lookup via `GET /api/products/barcode/{code}` |
| 6 | Expenses | `/api/expenses` (paid from the open register) |
| 7 | Returns | `/api/returns` (partial returns, pro-rated bill discount, auto restock) |
| 8 | Discounts (name, price, discounted price) | `discountedPrice` on products, `PUT /api/products/{id}/discount`, `?onlyDiscounted=true` |
| 9, 10, 11 | Add (with description), remove, list/search products | `/api/products` |
| 12 | Sales report per day / week / month / year | `GET /api/reports/sales?period=Daily\|Weekly\|Monthly\|Yearly&date=` |
| 13 | Quantity adjustment | `/api/stock-adjustments` (full stock ledger) |
| 14, 15 | Urdu instructions and QR on receipt | `GET /api/sales/{id}/receipt`, editable in `/api/settings` |

Full endpoint reference: [docs/API.md](docs/API.md). In Development the interactive reference is at `/scalar` and the
OpenAPI document at `/openapi/v1.json`.

## Architecture

Clean Architecture with dependencies pointing inwards:

```
Api ──► Infrastructure ──► Application ──► Domain
```

```
src/
├── Pos.Domain/                  Entities with behaviour and invariants, enums, DomainException
│   ├── Common/                  BaseEntity, AuditableEntity, Money, Guard, Roles
│   ├── Entities/                User, Category, Product, StockAdjustment, RegisterSession,
│   │                            Sale, SaleItem, SaleReturn, SaleReturnItem, Expense, StoreSettings
│   └── Enums/
├── Pos.Application/             Use cases; no EF Core or ASP.NET dependency
│   ├── Common/
│   │   ├── Abstractions/        Persistence (repositories, unit of work, report queries) and service interfaces
│   │   ├── Exceptions/          NotFound, Conflict, Validation, Forbidden, Unauthorized
│   │   ├── Models/              PagedResult, PageRequest, DateRange
│   │   └── Validation/          FluentValidation helpers
│   └── Features/<Feature>/      DTOs, validators, mappings (EF-translatable projections), I<Feature>Service, <Feature>Service
├── Pos.Infrastructure/          EF Core (SQL Server), repositories, JWT, password hashing, clocks, generators
│   ├── Persistence/
│   │   ├── Configurations/      IEntityTypeConfiguration per aggregate (indexes, precision, FK rules)
│   │   ├── Repositories/
│   │   ├── Queries/             Report aggregations pushed down to SQL
│   │   ├── Interceptors/        Audit timestamps
│   │   ├── Seeding/             Initial admin and optional demo catalogue
│   │   └── Migrations/
│   ├── Security/
│   └── Services/
└── Pos.Api/                     Thin controllers, JWT auth, ProblemDetails error handling, OpenAPI
tests/
└── Pos.Tests/                   Domain, application (checkout, returns, register close, reports) and HTTP smoke tests
```

### Design notes

- **Rich domain model.** `Sale.Create` prices lines from the product (list price, discounted price, cost snapshot) and
  computes totals, so totals can never disagree with items. `Sale.ProcessReturn` refunds what the customer actually
  paid, including their share of any bill discount, and caps refunds at the amount paid.
- **Server-side pricing.** Clients send only product ids and quantities; prices are never trusted from the client.
- **Consistency.** Checkout and returns run in a single transaction with the SQL Server retry strategy. Product stock
  uses an optimistic concurrency token, so two counters selling the last unit cannot both succeed (the second gets
  `409`). A filtered unique index guarantees at most one open register per user.
- **Stock ledger.** Every stock change (opening stock, manual adjustment, sale, return) writes a `StockAdjustment`
  row with before/after quantities and a reference to the sale or return.
- **Scalable reads.** Lists are paged and projected straight to DTOs (`AsNoTracking`, no over-fetching). Reports
  aggregate in SQL per local hour (at most 24 rows a day) and are rolled up to day/week/month in memory.
- **Time zones.** Timestamps are stored in UTC; day/week/month boundaries use the store time zone
  (`Store:TimeZoneId`, default `Asia/Karachi`).
- **Soft delete.** Deleted products disappear from the catalogue but stay in sales history and the stock ledger.
- **Errors.** All failures are RFC 7807 problem details; validation errors are keyed by camelCase field name.
- **Security.** PBKDF2 password hashing (ASP.NET Core Identity hasher), JWT bearer tokens with `Admin`/`Salesman`
  roles, rate-limited login, CORS allow-list, no internal error details in responses.

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2019+ (or run it with Docker, below)

### Run with Docker

```bash
docker compose up --build
```

The API listens on http://localhost:5080 with SQL Server on port 1433. Migrations run on start-up and a demo
catalogue is seeded.

### Run locally

1. Set the connection string and a signing key (user secrets keep them out of source control):

   ```bash
   cd src/Pos.Api
   dotnet user-secrets init
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=PosDb;Trusted_Connection=True;TrustServerCertificate=True"
   dotnet user-secrets set "Jwt:SigningKey" "<at least 32 random characters>"
   ```

2. Run the API (migrations are applied automatically on start-up):

   ```bash
   dotnet run --project src/Pos.Api
   ```

3. Open http://localhost:5080/scalar to explore the API.

### Default accounts

| Role | Username | Password |
|---|---|---|
| Admin | `admin` | `Admin@123` (from `Seed:AdminPassword`; change it after first login) |
| Salesman (demo data only) | `salesman` | `Sales@123` |

### Configuration

| Key | Purpose | Default |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | SQL Server connection | local SQL Server |
| `Jwt:SigningKey` | HMAC key, **required**, 32+ characters | empty (set it) |
| `Jwt:AccessTokenMinutes` | Token lifetime | `720` |
| `Store:TimeZoneId` | Store time zone for reports and receipts | `Asia/Karachi` |
| `Database:ApplyMigrationsOnStartup` | Apply EF migrations at start-up | `true` |
| `Seed:AdminUsername` / `Seed:AdminPassword` | First admin, created only when there are no users | `admin` / `Admin@123` |
| `Seed:DemoData` | Seed demo salesman, categories and products | `false` (`true` in Development) |
| `Cors:AllowedOrigins` | Allowed browser origins | `http://localhost:3000` |

Any key can be set with environment variables, e.g. `Jwt__SigningKey`.

## Development

```bash
dotnet build
dotnet test

# Add a migration (the EF tool is pinned in .config/dotnet-tools.json)
dotnet tool restore
dotnet ef migrations add <Name> -p src/Pos.Infrastructure -s src/Pos.Infrastructure -o Persistence/Migrations
```

Tests run against in-memory SQLite, so no database server is needed. CI runs build and tests on every push
(`.github/workflows/ci.yml`).
