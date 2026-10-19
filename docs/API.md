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
| `PaymentMethod` | `Cash`, `Card`, `Online` |
| `OnlinePaymentType` | `JazzCash`, `EasyPaisa`, `BankTransfer` |
| `RegisterStatus` | `Open`, `Closed` |
| `SaleStatus` | `Completed`, `PartiallyReturned`, `Returned` |
| `StockAdjustmentType` | `Initial`, `Manual`, `Sale`, `Return` |
| `ReportPeriod` | `Daily`, `Weekly`, `Monthly`, `Yearly` |
| `DiscountType` | `Percentage`, `Amount` |
| `MessageKind` | `ThankYou`, `Promotion` |
| `MessageStatus` | `Pending`, `Sent`, `Failed` |

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
  "isActive": true, "hasBarcodeImage": false, "barcodeImageUpdatedAt": null,
  "createdAt": "...", "updatedAt": "..." }
```
`effectivePrice` = `discountedPrice ?? salePrice` and is what a sale charges.
`barcodeImageUpdatedAt` changes whenever the barcode image is replaced; use it to bust image caches.

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/products?search=&categoryId=&onlyDiscounted=&lowStock=&includeInactive=&page=&pageSize=` | | `Paged<ProductDto>` (search matches name, description or exact barcode; `includeInactive` defaults to `true`) |
| GET | `/products/{id}` | | `ProductDto` |
| GET | `/products/barcode/{barcode}` | | `ProductDto` (`404` if unknown) |
| POST *(Admin)* | `/products` | `{ name, barcode?, description?, categoryId?, costPrice, salePrice, discountedPrice?, stockQuantity, reorderLevel }` | `201 ProductDto`. Empty barcode ⇒ an EAN-13 is generated. |
| PUT *(Admin)* | `/products/{id}` | `{ name, barcode?, description?, categoryId?, costPrice, salePrice, discountedPrice?, reorderLevel, isActive }` | `ProductDto` (stock changes only through adjustments) |
| PUT *(Admin)* | `/products/{id}/discount` | `{ discountedPrice: number \| null }` | `ProductDto` (`null` removes the discount) |
| GET | `/products/{id}/barcode-image` | | The image bytes with their `Content-Type` (`404` if the product has none) |
| PUT *(Admin)* | `/products/{id}/barcode-image` | `multipart/form-data` with one file field `file` | `ProductDto`. PNG, JPEG, WebP or GIF (detected from the file contents), at most 1 MB. Replaces any existing image. |
| DELETE *(Admin)* | `/products/{id}/barcode-image` | | `ProductDto` (removes the image; no-op if none) |
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
  "openingCash": 5000, "cashSales": 12500, "cardSales": 3000, "onlineSales": 2000, "cashRefunds": 400, "expenses": 600,
  "expectedCash": 16500, "closingCash": null, "difference": null,
  "saleCount": 23, "openingNotes": null, "closingNotes": null }
```
`expectedCash = openingCash + cashSales - cashRefunds - expenses` (card and online sales never reach the drawer); `difference = closingCash - expectedCash`.

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
  "customerName": null, "customerPhone": null, "customerId": null, "paymentMethod": "Cash", "onlinePaymentType": null,
  "subtotal": 1050, "itemDiscount": 90, "billDiscount": 10, "discountCode": null, "codeDiscount": 0, "total": 950,
  "amountPaid": 1000, "change": 50, "returnedAmount": 0, "status": "Completed",
  "createdAt": "...", "items": [ ... ] }
```
- `subtotal` = Σ `unitPrice × quantity` (list prices)
- `itemDiscount` = Σ `lineDiscount` (product discounted prices plus line discounts)
- `billDiscount` = the cashier's bill discount + `codeDiscount`
- `codeDiscount` is what `discountCode` took off; the code applies to the amount after item discounts and the cashier's bill discount
- `total` = `subtotal − itemDiscount − billDiscount`
- `onlinePaymentType` is set only when `paymentMethod` is `Online`
- `customerId` is set when the bill has a valid phone number (see Customers)

`SaleSummaryDto`: `{ id, invoiceNumber, cashierName, customerName, paymentMethod, onlinePaymentType, total, itemCount, status, createdAt }`

| Method | Path | Body | Response |
|---|---|---|---|
| POST | `/sales` | `{ items: [{ productId, quantity, discount? }], paymentMethod, onlinePaymentType?, amountPaid, billDiscount?, customerName?, customerPhone?, discountCode? }` | `201 SaleDto`. `onlinePaymentType` is required when `paymentMethod` is `Online` and ignored otherwise; card and online bills are recorded as paid in full. `customerPhone`, when given, must be a Pakistani mobile number (see Customers). Prices are always taken from the server. `discount` is an optional extra discount **per unit** (≥ 0, at most the product's discounted price); it is multiplied by `quantity`. `400` if stock is insufficient, `amountPaid < total` for cash, or the discount code cannot be used (inactive, expired, limit reached, bill below its minimum); `404` for an unknown code. A valid `customerPhone` finds or creates the customer and, when WhatsApp is on, queues the thank-you message. |
| GET | `/sales?search=&from=&to=&cashierId=&page=&pageSize=` | | `Paged<SaleSummaryDto>` (`search` matches invoice number / customer; `from`/`to` are `yyyy-MM-dd` store-local dates; salesmen only see their own) |
| GET | `/sales/{id}` | | `SaleDto` |
| GET | `/sales/invoice/{invoiceNumber}` | | `SaleDto` |
| GET | `/sales/{id}/receipt` | | `ReceiptDto` |

`ReceiptDto`: `{ store: StoreSettingsDto, sale: SaleDto, qrContent: string }`. `qrContent` is the text to encode into the receipt QR code: just the invoice number, so scanning it into the returns search finds the sale.

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
               "cashSales": 190000, "cardSales": 44000, "onlineSales": 10000,
               "jazzCashSales": 6000, "easyPaisaSales": 3000, "bankTransferSales": 1000, "averageSale": 2033.33 },
  "buckets": [ { "label": "01 Oct", "start": "2026-10-01T00:00:00", "saleCount": 4, "netSales": 8000, "returns": 0, "netRevenue": 8000 } ],
  "topProducts": [ { "productId": 1, "productName": "Basmati Rice 1kg", "quantity": 40, "revenue": 12800 } ],
  "cashiers": [ { "userId": 2, "userName": "Ali", "saleCount": 80, "total": 160000 } ],
  "expenseCategories": [ { "category": "Utilities", "count": 2, "total": 6000 }, { "category": null, "count": 1, "total": 2000 } ],
  "expenses": [ { "id": 7, "createdAt": "2026-10-02T09:15:00Z", "title": "Electricity bill", "category": "Utilities",
                  "amount": 5000, "createdBy": "Ali" } ] }
```
`grossSales` = Σ subtotal, `netSales` = gross − discounts, `netRevenue` = netSales − returns, `netProfit` = grossProfit − expenses.
`expenses` lists every expense recorded in the range (oldest first); `expenseCategories` totals them per category (largest first; `null` = no category).

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

## Customers

Customers are known by phone number, stored as international digits without the plus (`923001234567`). Phone numbers on bills and customers must be Pakistani mobile numbers: 11 digits starting with `03` (`03001234567`), or `+92` / `0092` / `92` followed by 10 digits starting with `3` (`+923001234567`), with a mobile network code in use: `030x` Jazz, `031x` Zong, `032x` Jazz (ex-Warid), `033x` Ufone, `034x` Telenor or `0355` SCO (so `0350…`, `036x`–`039x` are rejected). Spaces and dashes are ignored (`0300-1234567`, `+92 300 1234567`); anything else is a `400` validation error. A bill with a valid phone number finds or creates the customer automatically.

`CustomerDto`: `{ id, name, phone, optedOut, purchaseCount, totalSpent, lastPurchaseAt, createdAt }` (`totalSpent` is net of returns; `optedOut` customers get no promotions).

| Method | Path | Body | Response |
|---|---|---|---|
| GET *(Admin)* | `/customers?search=&optedOut=&page=&pageSize=` | | `Paged<CustomerDto>` (search matches name or phone; latest buyers first) |
| GET *(Admin)* | `/customers/{id}` | | `CustomerDto` |
| GET | `/customers/lookup?phone=` | | `200 { id, name, phone }` or `204` when unknown (used while billing) |
| POST *(Admin)* | `/customers` | `{ name?, phone, optedOut? }` | `201 CustomerDto` (`409` if the phone exists) |
| PUT *(Admin)* | `/customers/{id}` | `{ name?, phone, optedOut }` | `CustomerDto` |
| DELETE *(Admin)* | `/customers/{id}` | | `204` (their sales are kept) |

### WhatsApp promotions *(Admin)*

`PromotionDto`: `{ id, message, createdBy, createdAt, recipientCount, sentCount, failedCount, pendingCount }`

`OutgoingMessageDto`: `{ id, kind, phone, customerName, status, error, attempts, saleId, promotionId, createdAt, sentAt }`

| Method | Path | Body | Response |
|---|---|---|---|
| POST | `/customers/promotions` | `{ message, customerIds? }` (omit or empty = every customer who has not opted out) | `201 PromotionDto`. Messages are queued and sent in the background. `400` when WhatsApp is off or has no promotion template. |
| GET | `/customers/promotions?page=&pageSize=` | | `Paged<PromotionDto>` (newest first) |
| GET | `/customers/messages?kind=&status=&promotionId=&page=&pageSize=` | | `Paged<OutgoingMessageDto>` (newest first; `error` says why a message failed) |

Messages are WhatsApp **template** messages sent through the WhatsApp Business Cloud API. The templates are created and approved in Meta's WhatsApp Manager and must have these body variables:

- thank-you: `{{1}}` customer name (or "Customer"), `{{2}}` invoice number, `{{3}}` bill total (e.g. `Rs 2,500`)
- promotion: `{{1}}` customer name (or "Customer"), `{{2}}` the promotion text (line breaks become spaces)

Temporary failures (rate limits, network, Meta server errors) are retried up to 3 times; other failures are marked `Failed` with Meta's error.

## Discount codes

`DiscountCodeDto`: `{ id, code, description, type, value, minimumBill, maximumDiscount, validFrom, validTo, usageLimit, usedCount, isActive, createdAt }`

- `code` is stored upper-case (letters, digits, `-`, `_`); codes are matched case-insensitively.
- `value` is a percent (1–100) for `Percentage`, otherwise an amount. `maximumDiscount` caps a percentage code.
- `minimumBill` applies to the amount after item discounts and the cashier's bill discount. `validFrom`/`validTo` are inclusive store-local dates. `usedCount` counts sales that used the code.

| Method | Path | Body | Response |
|---|---|---|---|
| GET *(Admin)* | `/discount-codes?search=&page=&pageSize=` | | `Paged<DiscountCodeDto>` |
| GET *(Admin)* | `/discount-codes/{id}` | | `DiscountCodeDto` |
| GET | `/discount-codes/check?code=&amount=` | | `{ code, description, type, value, minimumBill, maximumDiscount, discount }` where `discount` is what the code takes off `amount`; `400` with the reason when it cannot be used, `404` when unknown |
| POST *(Admin)* | `/discount-codes` | `{ code, description?, type, value, minimumBill?, maximumDiscount?, validFrom?, validTo?, usageLimit?, isActive }` | `201 DiscountCodeDto` (`409` if the code exists) |
| PUT *(Admin)* | `/discount-codes/{id}` | same as POST | `DiscountCodeDto` |
| DELETE *(Admin)* | `/discount-codes/{id}` | | `204` (sales keep the code text) |

## Store settings

`StoreSettingsDto`: `{ storeName, address, phone, receiptFooter, urduInstructions, whatsAppGroupCaption, whatsAppGroupQrUpdatedAt, storeLogoUpdatedAt, receiptLogoUpdatedAt, currencySymbol, timeZoneId }`

`urduInstructions` is multi-line Urdu text (right-to-left) printed at the bottom of every receipt. `whatsAppGroupQrUpdatedAt` is `null` when no WhatsApp group QR code is uploaded, otherwise it changes whenever the image is replaced; the receipt prints that image with `whatsAppGroupCaption` under it (nothing when the caption is empty). `storeLogoUpdatedAt` and `receiptLogoUpdatedAt` work the same way for the store logo (shown in the app sidebar) and the optional receipt logo; a receipt prints the receipt logo, else the store logo, else the store name. `timeZoneId` is read-only and comes from the `Store:TimeZoneId` server setting.

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/settings` | | `StoreSettingsDto` (any signed-in user) |
| PUT *(Admin)* | `/settings` | `{ storeName, address?, phone?, receiptFooter?, urduInstructions?, whatsAppGroupCaption?, currencySymbol }` | `StoreSettingsDto` |
| GET | `/settings/{image}` | | The image bytes with their `Content-Type` (`404` if none). `{image}` is `store-logo`, `receipt-logo` or `whatsapp-group-qr`. |
| PUT *(Admin)* | `/settings/{image}` | `multipart/form-data` with one file field `file` | `StoreSettingsDto`. PNG, JPEG, WebP or GIF, at most 1 MB. |
| DELETE *(Admin)* | `/settings/{image}` | | `StoreSettingsDto` |

### WhatsApp messaging *(Admin)*

`WhatsAppSettingsDto`: `{ enabled, phoneNumberId, hasAccessToken, thankYouTemplate, promotionTemplate, templateLanguage, defaultCountryCode }`. The access token is never returned.

| Method | Path | Body | Response |
|---|---|---|---|
| GET | `/settings/whatsapp` | | `WhatsAppSettingsDto` |
| PUT | `/settings/whatsapp` | `{ enabled, phoneNumberId?, accessToken?, thankYouTemplate?, promotionTemplate?, templateLanguage, defaultCountryCode }` | `WhatsAppSettingsDto`. Omit or empty `accessToken` keeps the saved one. `enabled` needs a phone number ID and token. No thank-you template = no thank-you messages. |
| POST | `/settings/whatsapp/test` | `{ phone }` | `{ success, template, error }`. Sends one message now with the thank-you template (sample values), else the promotion template, else Meta's `hello_world`. |

The Graph API address can be changed with the `WhatsApp:GraphApiBaseUrl` server setting (default `https://graph.facebook.com/v23.0/`).

## Health
`GET /health` → `200 Healthy` (anonymous).
