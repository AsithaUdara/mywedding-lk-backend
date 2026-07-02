# MyWedding.lk — Backend

.NET 8 modular monolith API for the MyWedding.lk wedding planning platform.

## Structure

```
backend/
├── Core/                 Domain entities and repository interfaces
├── Shared/               Cross-cutting kernel (behaviors, exceptions)
├── Infrastructure/       EF Core DbContext, migrations, integrations
├── Presentation/         MyWedding.API (controllers, hubs, middleware)
├── Modules/              Bounded contexts (Identity, Events, Tasks, Budget, Vendors, Collaboration, Planner)
└── tests/                Domain + API integration tests
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB or full instance)
- Firebase project with service account JSON

## Quick start

```powershell
cd backend
copy Presentation\MyWedding.API\appsettings.example.json Presentation\MyWedding.API\appsettings.Development.json
# Set connection string, Firebase credentials — see docs/REAL_API_SETUP.md

cd Presentation\MyWedding.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
dotnet run
```

API: `http://localhost:5141` · Swagger (Development): `/swagger`

## Database

Migrations run automatically on startup when `Database:ApplyMigrationsOnStartup` is `true` (default).

```powershell
cd backend
dotnet ef database update --project Infrastructure\MyWedding.Infrastructure --startup-project Presentation\MyWedding.API
dotnet ef migrations add MigrationName --project Infrastructure\MyWedding.Infrastructure --startup-project Presentation\MyWedding.API
```

## Tests

```powershell
cd backend
dotnet test MyWedding.sln
```

| Project | Coverage |
|---------|----------|
| `MyWedding.Domain.Tests` | Core domain invariants (e.g. `WeddingEvent.Create`) |
| `MyWedding.API.IntegrationTests` | API smoke tests (`WebApplicationFactory`, EF InMemory, `Testing` environment) |

Integration tests use `appsettings.Testing.json` (no migrations, no seed, mock email) and the `Test` auth scheme (`Authorization: Test {userId}|{role}`). CI runs on push/PR via `.github/workflows/backend-ci.yml`.

## Configuration

| Key | Purpose |
|-----|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server |
| `Firebase:ProjectId` / `CredentialsPath` | Authentication |
| `Cors:AllowedOrigins` | Frontend URLs (defaults to `Frontend:BaseUrl`) |
| `Database:ApplyMigrationsOnStartup` | Auto-migrate on boot |
| `Database:SeedOnStartup` | Seed categories/reference data |
| `Integrations:AllowMockEmail` | Use mock email when SMTP is not configured |

API versioning defaults to v1 with `ReportApiVersions` enabled; routes stay at `/api/...` (no URL prefix change).

See `Presentation/MyWedding.API/appsettings.example.json` and [docs/REAL_API_SETUP.md](../docs/REAL_API_SETUP.md).

## Architecture

- **CQRS** via MediatR with FluentValidation pipeline
- **Modular monolith** — one deployable API, bounded contexts per module
- **Firebase** bearer authentication
- **SignalR** for collaboration and notifications
