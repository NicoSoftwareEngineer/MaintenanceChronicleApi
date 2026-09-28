# Maintenance Chronicle API

Maintenance Chronicle is a REST API for tracking customers, locations, machines, maintenance records, and reminders. It also supports user accounts, role-based access, and email workflows.

## What the API supports

- Managing customers, locations, and machines
- Recording maintenance work and scheduling reminders
- Managing users and their roles
- Signing in with JWT authentication
- Sending email confirmation, password reset, and invitation emails

## Getting started

### 1. Prerequisites

Install the .NET 9 SDK and make a PostgreSQL database available to the API. To use email features, you also need access to an SMTP server.

### 2. Get the source

```bash
git clone https://github.com/NicoSoftwareEngineer/MaintenanceChronicleApi.git
cd MaintenanceChronicleApi
```

### 3. Configure the API

Open `src/MaintenanceChronicle.Api/appsettings.json` and set the values for your environment. The example below shows the complete configuration structure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Pagination": {
    "DefaultPageSize": 5,
    "MaxPageSize": 50
  },
  "ConnectionStrings": {
    "DbConnection": "Host=localhost;Port=5432;Database=maintenance_chronicle;Username=YOUR_USER;Password=YOUR_PASSWORD"
  },
  "EnvironmentOptions": {
    "FrontendHostUrl": "YOUR_FRONTEND_URL",
    "FrontendConfirmUrl": "/auth/email-confirm/[Email]/[ConfToken]",
    "FrontendPasswordResetUrl": "/auth/password-reset/[Email]/[PasswordToken]",
    "FrontendPasswordCreateUrl": "/auth/create-password/[Email]/[ConfToken]/[PasswordToken]",
    "SenderEmail": "YOUR_SENDER_EMAIL",
    "SenderName": "Maintenance Chronicle"
  },
  "SmtpOptions": {
    "Host": "YOUR_SMTP_HOST",
    "Port": 587,
    "Username": "YOUR_SMTP_USERNAME",
    "Password": "YOUR_SMTP_PASSWORD"
  },
  "JwtOptions": {
    "SecretKey": "REPLACE_WITH_A_LONG_RANDOM_SECRET_KEY",
    "Issuer": "YOUR_API_URL",
    "Audience": "YOUR_FRONTEND_URL",
    "AccessTokenExpirationInMinutes": 30,
    "RefreshTokenExpirationInDays": 14
  }
}
```

Replace the uppercase placeholders with your own values. Set the SMTP port to the port required by your email provider.

Email confirmation, password reset, invitations, and reminder emails require working `SmtpOptions` settings. The links in those emails also depend on the frontend URLs in `EnvironmentOptions`. Keep passwords and signing keys private, and do not commit your filled-in configuration file with credentials.

### 4. Start the API

From the repository root:

```bash
dotnet run --project src/MaintenanceChronicle.Api --launch-profile http
```

The API will run at `http://localhost:5209`. In the Development environment, open [Swagger UI](http://localhost:5209/swagger) to explore and call its endpoints.

The API applies database migrations when it starts. Check that `DbConnection` points to your intended database before running it.

## First account and authentication

Use `POST /api/v1/auth/register-user-tenant` to create the first user and tenant. Registration does not sign the user in. The account must have its email confirmed before login, so SMTP must be configured for this flow.

Request a confirmation email with `POST /api/v1/auth/send-email-confirm-email`, then complete confirmation through `POST /api/v1/auth/validate-token`. Sign in with `POST /api/v1/auth/login`.

Login returns an access token. Send it as a Bearer token when calling protected endpoints. In Swagger UI, use **Authorize** and enter the token without the `Bearer` prefix.

## Programmer documentation

For the codebase guide, development setup, tests, and generated API reference, see the [Maintenance Chronicle programmer documentation](https://nicosoftwareengineer.github.io/MaintenanceChronicleApi/index.html).