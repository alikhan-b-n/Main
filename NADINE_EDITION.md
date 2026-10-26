# Nadine edition

Branch `Nadine_edition` turns Lama CRM into a stockist tracker for Noah's Adventures, a series of
children's picture books sold through shops on consignment. Frontend: `lama-crm-redesign`, same
branch name.

## What it tracks

A **stockist** is a store that carries the books, or a lead that might. Stages:
`Lead → Contacted → Interested` (opportunities), `Active` (has books), `Inactive` (stopped, kept for
its history). Recording the first delivery to a lead makes it `Active`.

| | |
|---|---|
| Stock | Copies of each title in the store now. Titles are fixed codes in `Lama.Domain/Stockists/BookTitles.cs` (`cork`, `galway`, `kerry`, `berlin`, `waterford`, `tipperary`). |
| Delivery / Return / Stock check | Change the stock and are written to the history. A stock check sets the number the store has now; the drop since the last number is what sold. |
| Contact log | A call, email or visit. Resets the "no contact" reminder. A plain note does not. |
| Invoices | Only the date sent, amount, reference and whether it is paid. Invoices are made elsewhere. |
| History | `StockistEvents`: every change, dated by the day it happened (`OccurredOn`). |

An **active** store needs a follow-up (`Lama.Domain/Stockists/StockistAttention.cs`) when:

- it has 4 or fewer books in total,
- nobody has been in touch for 90+ days (delivery, return, stock check or logged contact),
- an invoice has been unpaid for 60+ days.

## Endpoints

All require a signed-in user; the import needs an administrator.

| Method | Path | |
|---|---|---|
| GET | `/api/stockists?group=stores\|opportunities&stage=&title=&region=&search=&attention=true` | whole list, flagged stores first |
| GET | `/api/stockists/summary` | dashboard numbers and copies per title |
| GET | `/api/stockists/{id}` | store with stock, invoices and history |
| POST | `/api/stockists` | `{ name, region, address, contactName, email, phone, stage, note }` |
| PUT | `/api/stockists/{id}` | contact details |
| PATCH | `/api/stockists/{id}/stage` | `{ stage }` |
| POST | `/api/stockists/{id}/stock` | `{ movement: Delivery\|Return\|Count, title, quantity, on, note }` |
| POST | `/api/stockists/{id}/notes` | `{ text, contact, on }` |
| POST | `/api/stockists/{id}/invoices` | `{ sentOn, amount, reference }` |
| PATCH | `/api/stockists/{id}/invoices/{invoiceId}` | `{ paid, paidOn }` |
| DELETE | `/api/stockists/{id}/invoices/{invoiceId}` | |
| DELETE | `/api/stockists/{id}` | |
| POST | `/api/stockists/import` | Admin. List of stores; names already in the CRM are skipped |

## Moving the Excel sheet over

```bash
python3 -I tools/stockist-import/excel_to_import.py "Noahs Adventures- customer list.xlsx" stockists.json
```

Then, signed in as the administrator, post the file:

```bash
curl -X POST https://<host>/api/stockists/import -H "Authorization: Bearer <token>" -H "Content-Type: application/json" --data @stockists.json
```

Cells that are not a plain number or date (`11 - invoiced`, `July`, `removed`...) become notes on
the store, prefixed "From Excel", so nothing is lost. Dated deliveries set "last contact"; undated
stock does not. Running the import twice adds nothing.

## Database

Migration `AddStockists` adds `Stockists`, `StockistStock`, `StockistInvoices` and `StockistEvents`.
The tables of earlier editions (leads, appointments...) stay; their pages are hidden from the menu.

Set `Auth:Admin:FullName` to the owner's name; the default still says "IconicU Admin".
