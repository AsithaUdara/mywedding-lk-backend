# MyWedding.lk — Backend

REST API and **SignalR** real-time layer for **MyWedding.lk**, a B2B2C wedding planning platform. Handles identity, events, tasks, budget, vendor marketplace, planner CRM, collaboration, and payment webhooks.

Built as a **.NET 8 modular monolith** with CQRS (MediatR), EF Core, and Firebase JWT authentication.

**Companion repository:** [mywedding-lk-frontend](https://github.com/AsithaUdara/mywedding-lk-frontend) — Next.js web application.

---

## Features

| Module | Responsibilities |
|--------|------------------|
| **Identity** | User profiles, roles, Firebase token validation |
| **Events** | Wedding events, invitations, team collaboration |
| **Tasks** | Checklists and planner task workflows |
| **Budget** | Expense tracking and overview |
| **Vendors** | Marketplace listings, services, bookings, inquiries |
| **Planner** | CRM, clients, procurement, AI copilot, subscriptions |
| **Collaboration** | SignalR hubs for live checklist/budget updates |

---

## Tech stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 8 |
| API | ASP.NET Core, API versioning, Swagger (Development) |
| Data | EF Core, SQL Server |
| Patterns | CQRS + MediatR, FluentValidation pipeline |
| Auth | Firebase Admin SDK (service account JSON) |
| Real-time | SignalR |
| Integrations | OpenAI, PayHere, SMTP, Cloudinary (optional per environment) |

---

## Prerequisites

- **[.NET 8 SDK](https://dotnet.microsoft.com/download)**
- **SQL Server** (LocalDB, Express, or full instance)
- **Firebase** project with **service account** JSON (`firebase-credentials.json`)
- **Firebase Web API key** — for local PowerShell seed/test scripts only (via `FIREBASE_WEB_API_KEY` env var)

---

## Quick start

```powershell
git clone https://github.com/AsithaUdara/mywedding-lk-backend.git
cd mywedding-lk-backend

copy Presentation\MyWedding.API\appsettings.example.json Presentation\MyWedding.API\appsettings.Development.json
# Place firebase-credentials.json in Presentation\MyWedding.API\

cd Presentation\MyWedding.API

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=MyWeddingDb;Trusted_Connection=True;TrustServerCertificate=True;"

dotnet run
```

| Endpoint | URL |
|----------|-----|
| API | http://localhost:5141 |
| Swagger (Development) | http://localhost:5141/swagger |

Migrations apply automatically on startup when `Database:ApplyMigrationsOnStartup` is `true` (default in example config).

---

## Configuration

Use **`dotnet user-secrets`** for local development. Do not commit real credentials to git.

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `Firebase:ProjectId` | Firebase project ID |
| `Firebase:CredentialsPath` | Path to service account JSON (default: `firebase-credentials.json`) |
| `Cors:AllowedOrigins` | Allowed frontend origins |
| `Frontend:BaseUrl` | Frontend base URL for links and redirects |
| `OpenAI:ApiKey` | Planner AI copilot (mock mode when empty) |
| `PayHere:MerchantId` / `MerchantSecret` | Payment checkout signing |
| `Smtp:*` | Transactional email (or set `Integrations:AllowMockEmail=true`) |
| `Cloudinary:CloudName` / `ApiKey` / `ApiSecret` | Server-side PDF and media upload |
| `AdminBootstrap:Secret` | Local-only admin bootstrap endpoint guard |

See `Presentation/MyWedding.API/appsettings.example.json` for the full template.

### Firebase credentials file

1. Firebase Console → **Project settings** → **Service accounts** → **Generate new private key**.
2. Save as `Presentation/MyWedding.API/firebase-credentials.json`.
3. Confirm the file is **gitignored** and never pushed.

---

## Database

```powershell
# From repository root
dotnet ef database update --project Infrastructure\MyWedding.Infrastructure --startup-project Presentation\MyWedding.API

dotnet ef migrations add YourMigrationName --project Infrastructure\MyWedding.Infrastructure --startup-project Presentation\MyWedding.API
```

---

## Tests

```powershell
dotnet test MyWedding.sln
```

| Project | Scope |
|---------|--------|
| `MyWedding.Domain.Tests` | Domain invariants and unit tests |
| `MyWedding.API.IntegrationTests` | API smoke tests (`WebApplicationFactory`, in-memory EF, `Testing` environment) |

Integration tests use `appsettings.Testing.json` and the `Test` auth scheme: `Authorization: Test {userId}|{role}`.

---

## Demo & seed scripts

PowerShell scripts under `scripts/` seed demo data and run API happy-path tests. They authenticate against Firebase REST using a **Web API key** supplied at runtime — never hardcoded in source.

```powershell
# Set once per terminal session (use the same Web API key as the frontend Firebase config)
$env:FIREBASE_WEB_API_KEY = "YOUR_FIREBASE_WEB_API_KEY"

# Demo database bootstrap + full vendor seed
powershell -ExecutionPolicy Bypass -File scripts\seed_mwdemo_bootstrap.ps1
powershell -ExecutionPolicy Bypass -File scripts\seed_mwdemo_full.ps1

# API happy-path verification (API must be running on :5141)
powershell -ExecutionPolicy Bypass -File scripts\test_happy_path_round.ps1
```

Scripts that accept `-ApiKey` will fall back to `$env:FIREBASE_WEB_API_KEY`.

---

## Project structure

```
backend/
├── Core/                    # Domain entities and repository interfaces
├── Shared/                  # Cross-cutting kernel (behaviors, exceptions)
├── Infrastructure/          # EF Core DbContext, migrations, integrations
├── Presentation/
│   └── MyWedding.API/       # Controllers, hubs, middleware, startup
├── Modules/                 # Bounded contexts
│   ├── Identity/
│   ├── Events/
│   ├── Tasks/
│   ├── Budget/
│   ├── Vendors/
│   ├── Collaboration/
│   └── Planner/
├── scripts/                 # Seed and E2E test scripts (no secrets in repo)
└── tests/                   # Domain and integration tests
```

### Architecture notes

- **Modular monolith** — single deployable API, clear module boundaries.
- **CQRS** — commands and queries via MediatR with validation pipeline.
- **Firebase JWT** — bearer authentication on protected endpoints.
- **SignalR** — real-time collaboration for event workspaces.

---

## CI

GitHub Actions runs **restore**, **build**, and **test** on pushes and pull requests to `main`, `master`, and `develop`. No production secrets are required in CI.

---

## Security

- Never commit `firebase-credentials.json`, `appsettings.Development.json` with real values, or API keys in scripts.
- Use `FIREBASE_WEB_API_KEY` as an **environment variable** for local PowerShell scripts.
- If a key was ever committed to GitHub:
  1. **Rotate** the key in [Google Cloud Console](https://console.cloud.google.com/apis/credentials) / Firebase.
  2. **Push** the fix that removes the hardcoded value from the default branch.
  3. **Close** the Secret Scanning alert as **revoked**.
- Enable **Secret scanning push protection** under repository **Settings → Code security**.

Removing a secret from the latest commit does **not** erase it from git history. Rotation is mandatory; history rewriting is optional for compliance.

---

## Troubleshooting

| Issue | Fix |
|-------|-----|
| API won't start — SQL error | Check connection string; ensure SQL Server is running |
| 401 Unauthorized | Verify Firebase project matches frontend; token not expired |
| CORS blocked | Add frontend URL to `Cors:AllowedOrigins` |
| Seed script: `Set FIREBASE_WEB_API_KEY` | Export env var before running scripts |
| Migrations fail | Run `dotnet ef database update` manually; check DB permissions |
| Email not sending | Configure SMTP user-secrets or set `Integrations:AllowMockEmail=true` |

---

## API versioning

Default version is **v1** with `ReportApiVersions` enabled. Routes remain at `/api/...` (no URL version prefix).
