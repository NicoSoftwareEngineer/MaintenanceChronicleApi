#  MaintenanceChronicleApi

**MaintenanceChronicleApi** is a backend REST API built with **ASP.NET Core** using Clean architecture with **CQS (Command Query Separation)** pattern and backed by a **PostgreSQL** database. It is designed to handle tasks and workflows associated with maintenance tracking and management.

---

##  Tech Stack

- **ASP.NET Core Web API**
- **PostgreSQL**
- **Clean architecture with CQS pattern**
- **JWT Authentication**
- **SMTP Integration (for email workflows)**

---

##  Documentation

Comprehensive API and architecture documentation is available at:

 [https://nicosoftwareengineer.github.io/MaintenanceChronicleApi/index.html](https://nicosoftwareengineer.github.io/MaintenanceChronicleApi/index.html)

---

##  Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/NicoSoftwareEngineer/MaintenanceChronicleApi.git
cd maintenanceChronicleApi
```

### 2. Configure `appsettings.json`

Before running the project, ensure you fill in the necessary fields in the `appsettings.json` configuration file.

Here is a sample structure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DbConnection": "your_postgresql_connection_string"
  },
  "EnvironmentOptions": {
    "FrontendHostUrl": "url_of_ui",
    "FrontendConfirmUrl": "/auth/email-confirm/[Email]/[ConfToken]",
    "FrontendPasswordResetUrl": "/auth/password-reset/[Email]/[PasswordToken]",
    "FrontendPasswordCre_ateUrl": "/auth/create-password/[Email]/[ConfToken]/[PasswordToken]",
    "SenderEmail": "your_email",
    "SenderName": "Maintenance Chronicle"
  },
  "SmtpOptions": {
    "Host": "your_smtp_host",
    "Port": "your_smtp_port",
    "Username": "your_smtp_username",
    "Password": "your_smtp_password"
  },
  "JwtOptions": {
    "SecretKey": "your_very_long_secret_key",
    "Issuer": "url_of_app",
    "Audience": "url_of_ui",
    "AccessTokenExpirationInMinutes": 30,
    "RefreshTokenExpirationInDays": 14
  }
}
```

>  Replace all placeholder values with your own credentials.

---

##  Database Setup

Ensure you have a running PostgreSQL instance. Update the `DbConnection` string in `appsettings.json` accordingly.

Run the migrations to create the database schema:

```bash
dotnet ef database update
```

---

##  Running the Project

Use the .NET CLI to run the API locally:

```bash
dotnet run
```

The API will start on the default port (usually `https://localhost:5209`).

---

##  Architectural Overview

This project follows the **CQS (Command Query Separation)** pattern:

- **Commands**: Used for write operations (e.g., create/update/delete).
- **Queries**: Used for read operations (e.g., fetch by ID, list).

This separation promotes cleaner logic, better testability, and a more maintainable codebase.

---

##  Email Functionality

The API includes SMTP integration for:

- Email confirmation
- Password reset
- Password creation

Configure the `SmtpOptions` and `EnvironmentOptions` in `appsettings.json` to match your email provider and frontend URLs.

---

##  Authentication

Authentication is handled via **JWT (JSON Web Tokens)**. Ensure the `JwtOptions.SecretKey` is secure and sufficiently long.

#### I am retaking a course and I have work in a different repository --- what should I do?
If you did pass the requirements for the final project in a previous year, you don't need to repeat that part of the course again. If you didn't pass this requirement, you are, in principle, starting with no progress on the final project. Therefore, follow the current year's deadlines, the current year's *relevant teacher*, and manually transfer your work (specification, implementation) from the other repository to this one (including merge requests).
