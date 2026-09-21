# IconicU edition

Branch `IconicU_edition` turns Lama CRM into the back office for the IconicU Telegram bot
(repository `IconicUTelegramBot`): completed surveys arrive as **leads** and move through a
consultation funnel.

## What was added

| Layer | Files |
|---|---|
| Domain | `Lama.Domain/LeadManagement/Entities` — `Lead` aggregate, `LeadEvent` timeline, `LeadSubmission` |
| Application | `Lama.Application/LeadManagement` — submit / status / note / delete commands, list / details / stats queries, validators |
| Infrastructure | `LeadRepository`, `LeadConfiguration`, migration `AddLeads` (tables `Leads`, `LeadEvents`) |
| API | `TelegramLeadsController` (bot intake), `LeadsController` (UI), `TelegramBotApiKeyFilter` |
| Tests | `Lama.Tests` (xUnit) |

Qualification fields store the bot's option codes (`master`, `3000_5000`, `de`...), not text —
the frontend translates them. Keep the codes in sync with `IconicUTelegramBot/app/survey.py`.

## Funnel

`New → Contacted → ConsultationScheduled → ConsultationDone → ContractSigned`, or `Lost`
(a reason is required). Every change, note and repeated survey is recorded in `LeadEvents`.

## Bot intake

`POST /api/integrations/telegram/leads` accepts the bot's `Lead.to_crm_payload()` JSON as-is.

- Auth: `Authorization: Bearer <key>` (what the bot sends as `CRM_API_KEY`) or `X-Api-Key: <key>`.
  The key is `Integrations:TelegramBot:ApiKey`; while it is empty the endpoint answers **503**
  and never accepts leads anonymously.
- `201` — new lead; `200` — an open lead of the same Telegram user / email was updated, or the
  same `lead_id` was already stored (bot retry); `400` — invalid payload (the bot does not retry);
  `401` — wrong key.

Set the key outside `appsettings.json`, e.g.:

```bash
dotnet user-secrets init --project Lama.Api
```

```bash
dotnet user-secrets set "Integrations:TelegramBot:ApiKey" "<long random string>" --project Lama.Api
```

or the environment variable `Integrations__TelegramBot__ApiKey`. Put the same value into the
bot's `.env` as `CRM_API_KEY`, and point `CRM_WEBHOOK_URL` at the endpoint above.

## UI endpoints

| Method | Path | |
|---|---|---|
| GET | `/api/leads?status=&temperature=&source=&degree=&country=&search=&from=&to=&page=&pageSize=` | paged list |
| GET | `/api/leads/stats` | dashboard numbers |
| GET | `/api/leads/{id}` | card with timeline |
| PATCH | `/api/leads/{id}/status` | `{ "status": "Lost", "lostReason": "..." }` |
| POST | `/api/leads/{id}/notes` | `{ "text": "..." }` |
| DELETE | `/api/leads/{id}` | |

These, like the rest of the CRM, have no authentication yet — do not expose the API publicly
before adding it.

## Database

`dotnet-ef` is pinned as a local tool (`.config/dotnet-tools.json`):

```bash
dotnet tool restore
```

```bash
dotnet ef database update --project Lama.Infrastructure --startup-project Lama.Api
```

## Tests

```bash
dotnet test Lama.Tests
```
