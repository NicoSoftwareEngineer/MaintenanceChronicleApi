# Maintenance Chronicle developer documentation

Maintenance Chronicle is a .NET 9 REST API for managing customers, locations, machines, maintenance records, and reminders. It uses ASP.NET Core, PostgreSQL, and command/query handlers.

This documentation is for developers working on the API. The [API reference](docs/) contains documentation generated from the source projects.

## Codebase guide

| Project | Responsibility |
| --- | --- |
| `MaintenanceChronicle.Api` | HTTP endpoints, authentication setup, and application startup |
| `MaintenanceChronicle.Application` | Application use cases implemented as command handlers and queries |
| `MaintenanceChronicle.Application.Contracts` | Requests and shared application contracts |
| `MaintenanceChronicle.Data` | EF Core context, entities, repositories, specifications, and migrations |
| `MaintenanceChronicle.Infrastructure` | Persistence interfaces and service registration contracts |
| `MaintenanceChronicle.BackgroundServices` | Email and maintenance reminder workers |
| `MaintenanceChronicle.Utilities` | Shared options, error handling, and utilities |
| `MaintenanceChronicle.Application.Tests` | Application handler tests |
| `MaintenanceChronicle.Integration.Tests` | Integration tests using a PostgreSQL test container |
| `MaintenanceChronicle.Configurations` | Currently contains only a project file and is not included in the solution |

The solution and projects are under `src/`. To trace an API operation, start at its controller, follow the request to its application handler, and inspect the persistence code the handler calls.

## Local setup

### Prerequisites

- .NET 9 SDK
- PostgreSQL
- Docker, if you want to run the integration tests

### Configure the API

Use a PostgreSQL database intended for local development. The API reads its configuration from `src/MaintenanceChronicle.Api/appsettings.json` and standard ASP.NET Core configuration sources.

Set your database connection string and a development JWT signing key with .NET user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DbConnection" "Host=localhost;Port=5432;Database=maintenance_chronicle;Username=YOUR_USER;Password=YOUR_PASSWORD" --project src/MaintenanceChronicle.Api
dotnet user-secrets set "JwtOptions:SecretKey" "YOUR_LONG_RANDOM_DEVELOPMENT_SECRET" --project src/MaintenanceChronicle.Api
```

Review `JwtOptions:Issuer` and `JwtOptions:Audience` for your local client. Check the values in `EnvironmentOptions` for frontend links and sender details.

Email features require a working SMTP server. Configure `SmtpOptions:Host`, `SmtpOptions:Port`, `SmtpOptions:Username`, and `SmtpOptions:Password`. Also set `EnvironmentOptions:SenderEmail` and `EnvironmentOptions:SenderName` to the sender details you intend to use. The checked-in SMTP values are empty, so email delivery will not work without this configuration. Keep SMTP credentials in user secrets or another local secret store.

### Run the API

From the repository root:

```bash
dotnet run --project src/MaintenanceChronicle.Api --launch-profile http
```

With this launch profile, the API runs at `http://localhost:5209`. Swagger UI is available at `http://localhost:5209/swagger` in the Development environment.

The API applies EF Core migrations on startup. Make sure its connection string points to the intended local database before running it.

## Build and test

From the repository root:

```bash
dotnet build src/MaintenanceChronicle.sln
dotnet test src/MaintenanceChronicle.Application.Tests/MaintenanceChronicle.Application.Tests.csproj
dotnet test src/MaintenanceChronicle.Integration.Tests/MaintenanceChronicle.Integration.Tests.csproj
```

The integration tests start a PostgreSQL container and require Docker to be running.

## Documentation site

DocFX builds this site from `Documentation/docfx.json` and generates the API reference from projects under `src/`:

```bash
docfx Documentation/docfx.json
```