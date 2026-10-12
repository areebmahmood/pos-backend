# POS API Reference

Base URL: `http://localhost:5080/api` (configurable). All request/response bodies are JSON with **camelCase** properties.
Enums are serialized as **strings**. Timestamps are ISO-8601 UTC (`2026-10-10T09:30:00Z`). Money and quantities are decimals (JSON numbers).

## Conventions

- **Auth**: `Authorization: Bearer <jwt>` on every endpoint except `POST /auth/login` and `GET /health`.
- **Roles**: `Admin`, `Salesman`. Endpoints marked *(Admin)* return `403` for salesmen.
- **Paging**: list endpoints accept `page` (1-based, default 1) and `pageSize` (default 20, max 100) and return:
  ```json
  { "items": [], "page": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0 }
  ```
- **Errors** are RFC 7807 problem details:
  ```json
  { "type": "...", "title": "Validation failed", "status": 400, "detail": "...", "errors": { "salePrice": ["Must be greater than 0."] } }
  ```
  Status codes: `400` validation / business rule, `401` not signed in, `403` forbidden, `404` not found, `409` conflict (duplicate barcode, register already open, concurrent stock update).

## Enums

| Enum | Values |
|---|---|
| `UserRole` | `Admin`, `Salesman` |
| `PaymentMethod` | `Cash`, `Card` |
| `RegisterStatus` | `Open`, `Closed` |
| `SaleStatus` | `Completed`, `PartiallyReturned`, `Returned` |
| `StockAdjustmentType` | `Initial`, `Manual`, `Sale`, `Return` |
| `ReportPeriod` | `Daily`, `Weekly`, `Monthly`, `Yearly` |

## Auth

### `POST /auth/login`
Request `{ "username": "admin", "password": "Admin@123" }`
Response `200`:
```json
{ "accessToken": "eyJ...", "expiresAt": "2026-10-11T09:30:00Z",
  "user": { "id": 1, "username": "admin", "fullName": "Store Admin", "role": "Admin", "isActive": true, "createdAt": "..." } }
```
`401` on bad credentials or inactive user.

### `GET /auth/me` → `UserDto`

## Users *(Admin)*

`UserDto`: `{ id, username, fullName, role, isActive, createdAt }`

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/users?search=&page=&pageSize=` | | `Paged<UserDto>` |
| GET | `/users/{id}` | | `UserDto` |
| POST | `/users` | `{ username, fullName, password, role }` | `201 UserDto` |
| PUT | `/users/{id}` | `{ fullName, role, isActive, password? }` (password optional = keep) | `UserDto` |
| DELETE | `/users/{id}` | | `204` (deactivates; you cannot delete yourself) |

## Categories

`CategoryDto`: `{ id, name, description, productCount }`

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/categories` | | `CategoryDto[]` |
| POST *(Admin)* | `/categories` | `{ name, description? }` | `201 CategoryDto` |
| PUT *(Admin)* | `/categories/{id}` | `{ name, description? }` | `CategoryDto` |
| DELETE *(Admin)* | `/categories/{id}` | | `204` (`409` if it still has products) |

## Products

`ProductDto`:
```json
{ "id": 1, "name": "Basmati Rice 1kg", "barcode": "2000000000015", "description": "Premium long grain",
  "categoryId": 2, "categoryName": "Grocery",
  "costPrice": 280, "salePrice": 350, "discountedPrice": 320,
  "effectivePrice": 320, "discountPercent": 8.57,
  "stockQuantity": 40, "reorderLevel": 10, "isLowStock": false,
  "isActive": true, "createdAt": "...", "updatedAt": "..." }
```
`effectivePrice` = `discountedPrice ?? salePrice` and is what a sale charges.

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/products?search=&categoryId=&onlyDiscounted=&lowStock=&includeInactive=&page=&pageSize=` | | `Paged<ProductDto>` (search matches name, description or exact barcode; `includeInactive` defaults to `true`) |
| GET | `/products/{id}` | | `ProductDto` |
| GET | `/products/barcode/{barcode}` | | `ProductDto` (`404` if unknown) |
| POST *(Admin)* | `/products` | `{ name, barcode?, description?, categoryId?, costPrice, salePrice, discountedPrice?, stockQuantity, reorderLevel }` | `201 ProductDto`. Empty barcode ⇒ an EAN-13 is generated. |
| PUT *(Admin)* | `/products/{id}` | `{ name, barcode?, description?, categoryId?, costPrice, salePrice, discountedPrice?, reorderLevel, isActive }` | `ProductDto` (stock changes only through adjustments) |
| PUT *(Admin)* | `/products/{id}/discount` | `{ discountedPrice: number \| null }` | `ProductDto` (`null` removes the discount) |
| DELETE *(Admin)* | `/products/{id}` | | `204` (soft delete; history is kept) |

## Stock adjustments (quantity adjustment)

`StockAdjustmentDto`: `{ id, productId, productName, type, quantityChange, quantityBefore, quantityAfter, reason, userName, createdAt }`

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/stock-adjustments?productId=&page=&pageSize=` | | `Paged<StockAdjustmentDto>` |
| POST *(Admin)* | `/stock-adjustments` | `{ productId, quantityChange, reason }` (`quantityChange` is signed and non-zero) | `201 StockAdjustmentDto` |

## Register (cash in hand)

Each user has at most one open register. Sales, returns and cash expenses require the caller to have an open register.

`RegisterSessionDto`:
```json
{ "id": 7, "userId": 2, "userName": "Ali (salesman)", "status": "Open",
  "openedAt": "...", "closedAt": null,
  "openingCash": 5000, "cashSales": 12500, "cardSales": 3000, "cashRefunds": 400, "expenses": 600,
  "expectedCash": 16500, "closingCash": null, "difference": null,
  "saleCount": 23, "openingNotes": null, "closingNotes": null }
```
`expectedCash = openingCash + cashSales - cashRefunds - expenses`; `difference = closingCash - expectedCash`.

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/registers/current` | | `200 RegisterSessionDto` or `204` when the caller has no open register |
| POST | `/registers/open` | `{ openingCash, notes? }` | `201 RegisterSessionDto` (`409` if already open) |
| POST | `/registers/{id}/close` | `{ closingCash, notes? }` | `RegisterSessionDto` (own register; Admin may close any) |
| GET | `/registers?status=&userId=&page=&pageSize=` | | `Paged<RegisterSessionDto>` (salesmen only see their own) |
| GET | `/registers/{id}` | | `RegisterSessionDto` |

## Sales (billing)

`SaleItemDto`: `{ id, productId, productName, barcode, quantity, unitPrice, discountedUnitPrice, unitDiscount, manualDiscount, lineDiscount, lineTotal, returnedQuantity }`

- `unitDiscount` is the extra discount per unit the cashier gave; `manualDiscount` = `unitDiscount` × `quantity`.
- `lineDiscount` = (`unitPrice` − `discountedUnitPrice`) × `quantity` + `manualDiscount`
- `lineTotal` = `discountedUnitPrice` × `quantity` − `manualDiscount`

`SaleDto`:
```json
{ "id": 10, "invoiceNumber": "INV-261010-000010", "registerSessionId": 7, "cashierName": "Ali",
  "customerName": null, "customerPhone": null, "paymentMethod": "Cash",
  "subtotal": 1050, "itemDiscount": 90, "billDiscount": 10, "total": 950,
  "amountPaid": 1000, "change": 50, "returnedAmount": 0, "status": "Completed",
  "createdAt": "...", "items": [ ... ] }
```
- `subtotal` = Σ `unitPrice × quantity` (list prices)
- `itemDiscount` = Σ `lineDiscount` (product discounted prices plus line discounts)
- `total` = `subtotal − itemDiscount − billDiscount`

`SaleSummaryDto`: `{ id, invoiceNumber, cashierName, customerName, paymentMethod, total, itemCount, status, createdAt }`

| Method | Path | Body | Response |
|---|---|---|---|
| POST | `/sales` | `{ items: [{ productId, quantity, discount? }], paymentMethod, amountPaid, billDiscount?, customerName?, customerPhone? }` | `201 SaleDto`. Prices are always taken from the server. `discount` is an optional extra discount **per unit** (≥ 0, at most the product's discounted price); it is multiplied by `quantity`. `400` if stock is insufficient or `amountPaid < total` for cash. |
| GET | `/sales?search=&from=&to=&cashierId=&page=&pageSize=` | | `Paged<SaleSummaryDto>` (`search` matches invoice number / customer; `from`/`to` are `yyyy-MM-dd` store-local dates; salesmen only see their own) |
| GET | `/sales/{id}` | | `SaleDto` |
| GET | `/sales/invoice/{invoiceNumber}` | | `SaleDto` |
| GET | `/sales/{id}/receipt` | | `ReceiptDto` |

`ReceiptDto`: `{ store: StoreSettingsDto, sale: SaleDto, qrContent: string }`. `qrContent` is the text to encode into the receipt QR code.

## Returns

`SaleReturnDto`: `{ id, saleId, invoiceNumber, refundAmount, reason, processedBy, createdAt, items: [{ saleItemId, productId, productName, quantity, unitPrice, lineTotal }] }`

`unitPrice` here is the price actually paid per unit, including the share of any bill discount.

| Method | Path | Body | Response |
|---|---|---|---|
| POST | `/returns` | `{ saleId, reason?, items: [{ saleItemId, quantity }] }` | `201 SaleReturnDto`. Restocks items and refunds cash from the caller's open register. |
| GET | `/returns?search=&from=&to=&page=&pageSize=` | | `Paged<SaleReturnDto>` |
| GET | `/returns/{id}` | | `SaleReturnDto` |

## Expenses

`ExpenseDto`: `{ id, title, category, amount, notes, registerSessionId, createdBy, createdAt }`

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/expenses?search=&from=&to=&page=&pageSize=` | | `Paged<ExpenseDto>` (salesmen only see their own) |
| POST | `/expenses` | `{ title, category?, amount, notes? }` | `201 ExpenseDto`. Paid from the caller's open register (required). |
| DELETE *(Admin)* | `/expenses/{id}` | | `204` |

## Reports *(Admin)*

### `GET /reports/sales?period=Daily|Weekly|Monthly|Yearly&date=yyyy-MM-dd`
`date` defaults to today (store time zone). The range is the day, ISO week (Mon–Sun), month or year containing `date`.
Buckets: `Daily` → per hour, `Weekly`/`Monthly` → per day, `Yearly` → per month.

```json
{ "period": "Monthly", "from": "2026-10-01", "to": "2026-10-31",
  "summary": { "saleCount": 120, "returnCount": 3, "itemsSold": 410,
               "grossSales": 250000, "itemDiscounts": 5000, "billDiscounts": 1000,
               "netSales": 244000, "returns": 2000, "netRevenue": 242000,
               "costOfGoods": 180000, "grossProfit": 62000, "expenses": 8000, "netProfit": 54000,
               "cashSales": 200000, "cardSales": 44000, "averageSale": 2033.33 },
  "buckets": [ { "label": "01 Oct", "start": "2026-10-01T00:00:00", "saleCount": 4, "netSales": 8000, "returns": 0, "netRevenue": 8000 } ],
  "topProducts": [ { "productId": 1, "productName": "Basmati Rice 1kg", "quantity": 40, "revenue": 12800 } ],
  "cashiers": [ { "userId": 2, "userName": "Ali", "saleCount": 80, "total": 160000 } ] }
```
`grossSales` = Σ subtotal, `netSales` = gross − discounts, `netRevenue` = netSales − returns.

## Dashboards

### `GET /dashboard/admin` *(Admin)*
```json
{ "today": { "netSales": 0, "saleCount": 0, "returns": 0, "expenses": 0 },
  "month": { "netSales": 0, "saleCount": 0, "returns": 0, "expenses": 0 },
  "totalProducts": 0, "lowStockCount": 0,
  "lowStockProducts": [ProductDto], "openRegisters": [RegisterSessionDto],
  "recentSales": [SaleSummaryDto], "last7Days": [ { "date": "2026-10-04", "netSales": 0, "saleCount": 0 } ] }
```

### `GET /dashboard/salesman`
```json
{ "register": RegisterSessionDto | null,
  "today": { "netSales": 0, "saleCount": 0, "returns": 0, "expenses": 0 },
  "recentSales": [SaleSummaryDto] }
```

## Store settings

`StoreSettingsDto`: `{ storeName, address, phone, receiptFooter, urduInstructions, receiptQrBaseUrl, currencySymbol, timeZoneId }`

`urduInstructions` is multi-line Urdu text (right-to-left) printed at the bottom of every receipt. `timeZoneId` is read-only and comes from the `Store:TimeZoneId` server setting.

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/settings` | | `StoreSettingsDto` (any signed-in user) |
| PUT *(Admin)* | `/settings` | `{ storeName, address?, phone?, receiptFooter?, urduInstructions?, receiptQrBaseUrl?, currencySymbol }` | `StoreSettingsDto` |

## Health
`GET /health` → `200 Healthy` (anonymous).
