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
| Access control | `Lama.Domain/AccessControl`, `Lama.Application/AccessControl`, `AuthController`, `UsersController` |
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

## Sign-in and users

Everything except the bot intake and `POST /api/auth/login` requires a signed-in user:
the API issues a JWT on sign-in and the SPA sends it as `Authorization: Bearer <token>`.
Accounts are created by an administrator inside the CRM — there is no sign-up.

Roles: **Admin** (everything, including `/api/users`) and **Manager** (leads and contacts).
The last active administrator cannot be deleted, demoted or deactivated, and an administrator
cannot remove their own access.

The administrator account and the token signing key come from configuration, so they never
reach the repository:

```bash
dotnet user-secrets set "Auth:Admin:Email" "you@iconicu.kz" --project Lama.Api
```

```bash
dotnet user-secrets set "Auth:Admin:Password" "<at least 8 characters>" --project Lama.Api
```

```bash
dotnet user-secrets set "Auth:Jwt:Key" "<random 32+ characters>" --project Lama.Api
```

On startup the account is created if missing, and realigned if the secret changed (password,
role or a disabled flag). Change the admin password by updating the secret and restarting,
or from the admin panel itself. Without `Auth:Jwt:Key` the API signs tokens with a random key,
so everyone is signed out on restart; without the admin secrets nobody can sign in at all.

| Method | Path | |
|---|---|---|
| POST | `/api/auth/login` | email + password → token |
| GET | `/api/auth/me` | current user |
| POST | `/api/auth/password` | change own password |
| GET/POST | `/api/users` | list / create (Admin) |
| PUT | `/api/users/{id}` | name, role, access (Admin) |
| POST | `/api/users/{id}/password` | set a new password (Admin) |
| DELETE | `/api/users/{id}` | (Admin) |

Passwords are stored as PBKDF2-HMAC-SHA256 hashes (210 000 iterations, random salt per password).

## UI endpoints

| Method | Path | |
|---|---|---|
| GET | `/api/leads?status=&temperature=&source=&degree=&country=&search=&from=&to=&page=&pageSize=` | paged list |
| GET | `/api/leads/stats` | dashboard numbers |
| GET | `/api/leads/{id}` | card with timeline |
| PATCH | `/api/leads/{id}/status` | `{ "status": "Lost", "lostReason": "..." }` |
| POST | `/api/leads/{id}/notes` | `{ "text": "..." }` |
| DELETE | `/api/leads/{id}` | |

These require a signed-in user (any role).

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
