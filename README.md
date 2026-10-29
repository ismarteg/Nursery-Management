# Nursery Management

ASP.NET Core Web API + Angular SPA for nursery operations: children & QR/barcode entry, classes, payments, expenses, attendance-based salaries, RBAC, and audit logs.

## Structure

```
docs/                 Project plan and local HTTPS guide
backend/              ASP.NET Core API (EF Core, SQLite by default)
frontend/nursery-app/ Angular SPA (Arabic default + English)
```

## Prerequisites

- .NET 10 SDK
- Node.js 20+
- Angular CLI 20

## Quick start

### API

```powershell
cd backend/NurseryManagement.Api
dotnet restore
dotnet run --launch-profile https
```

- Swagger: https://localhost:7267/swagger  
- Default admin: `admin` / `Admin@123`

### Angular

```powershell
cd frontend/nursery-app
npm install
ng serve --ssl
```

- App: https://localhost:4200  

See [docs/local-https.md](docs/local-https.md) for trusting the local SSL certificate.

### Database

Default provider is **SQLite**. To switch later to **SQL Server**, set in `appsettings.json`:

```json
"Database": {
  "Provider": "SqlServer",
  "ConnectionString": "Server=.;Database=NurseryManagement;Trusted_Connection=True;TrustServerCertificate=True"
}
```

## Docs

- [docs/PLAN.md](docs/PLAN.md) — full product plan
- [docs/local-https.md](docs/local-https.md) — local SSL

## Remote

https://github.com/ismarteg/Nursery-Management.git
