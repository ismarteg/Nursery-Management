---
name: Nursery Management App
overview: "Greenfield nursery management system: ASP.NET Core Web API + EF Core (SQLite now, SQL Server-ready) + Angular SPA — local HTTPS/SSL, Arabic-default bilingual UI, RBAC, audit logs, children/QR entry, payments, expenses, classes, attendance-based salaries. Plan saved in-repo; GitHub remote ismarteg/Nursery-Management."
todos:
  - id: save-plan-doc
    content: Save plan as docs/PLAN.md in the workspace root
    status: in_progress
  - id: git-remote
    content: Init git (if needed), set remote https://github.com/ismarteg/Nursery-Management.git, push initial scaffold
    status: pending
  - id: scaffold
    content: Create backend/ (ASP.NET Core API) and frontend/ (Angular) as separate folders in workspace
    status: pending
  - id: local-ssl
    content: Local HTTPS with trusted SSL cert for API and Angular (dotnet dev-certs / mkcert)
    status: pending
  - id: db-provider-switch
    content: Config-driven EF provider (Sqlite|SqlServer) and portable schema for later SQL Server cutover
    status: pending
  - id: i18n-rtl
    content: Arabic-default + English i18n with RTL/LTR layout switching
    status: pending
  - id: roles-rbac
    content: Roles/permissions model, admin UI to manage roles, API authorization policies
    status: pending
  - id: audit-log
    content: Audit log middleware/service, persist who/what/when, admin audit viewer
    status: pending
  - id: classes-staff-children
    content: Classes, Staff, Children CRUD with QR/barcode generation and printable card
    status: pending
  - id: entry-desk
    content: Entry scan API + Angular entry desk and history
    status: pending
  - id: plans-payments
    content: Price plans and payments module
    status: pending
  - id: expenses
    content: Expense categories and expenses module
    status: pending
  - id: attendance-salaries
    content: CSV attendance upload and monthly salary calculation
    status: completed
  - id: dashboard
    content: Dashboard summary (children present, receivables, expenses, payroll)
    status: completed
isProject: false
---

# Nursery Management (.NET Core + SQLite + Angular)

## Stack (locked)
- **Backend:** ASP.NET Core 8 Web API, EF Core, **SQLite for local/dev**, JWT auth
- **Database later:** **SQL Server** via same EF Core model (provider switch by config)
- **Frontend:** Angular (standalone components), Angular Material
- **Local HTTPS:** trusted SSL certificate for API + Angular on localhost
- **UI language:** Arabic **default** + English; RTL when `ar`, LTR when `en`
- **Auth:** JWT + RBAC
- **Audit:** server-side audit log
- **Git remote:** [https://github.com/ismarteg/Nursery-Management.git](https://github.com/ismarteg/Nursery-Management.git)
- Parents portal deferred

## Workspace & folders (locked)

Root: `d:\work\nursery management\nursery management`

```
nursery management/                 # workspace root = git repo root
  docs/
    PLAN.md                         # this plan saved as project document
    local-https.md                  # SSL setup commands
  backend/
    NurseryManagement.Api/          # ASP.NET Core Web API
    NurseryManagement.sln           # solution file
  frontend/
    nursery-app/                    # Angular SPA
  .gitignore
  README.md
```

- API and Angular live in **separate top-level folders** (`backend/`, `frontend/`)
- Plan is also persisted as **[docs/PLAN.md](docs/PLAN.md)** in the repo (not only Cursor’s plan file)

## Git setup (first execution step)

1. `git init` in workspace root if not already a repo
2. Add `.gitignore` (bin/obj, node_modules, `*.db`, `frontend/certs/`, user secrets, etc.)
3. `git remote add origin https://github.com/ismarteg/Nursery-Management.git` (or `set-url` if origin exists)
4. After scaffold + `docs/PLAN.md`: commit and `git push -u origin main` (or `master` if remote default differs)
5. Requires GitHub auth on this machine (`gh auth` / credential manager); do not force-push

## Local SSL (HTTPS on localhost)

- API HTTPS via `launchSettings.json` + `dotnet dev-certs https --trust`
- Angular `ng serve --ssl` with certs under `frontend/certs/` (gitignored) or exported ASP.NET dev cert
- CORS allows Angular HTTPS origin
- Documented in `docs/local-https.md`

## Database: SQLite now, SQL Server later

```json
"Database": {
  "Provider": "Sqlite",
  "ConnectionString": "Data Source=nursery.db"
}
```

- Config switch `Sqlite` | `SqlServer`; both EF packages referenced; portable schema only

## Domain model

```mermaid
erDiagram
  Role ||--o{ UserRole : assigns
  User ||--o{ UserRole : has
  Role ||--o{ RolePermission : grants
  Permission ||--o{ RolePermission : maps
  User ||--o{ AuditLog : performs
  Class ||--o{ Child : enrolls
  Class ||--o{ StaffAssignment : has
  Child ||--o{ EntryRecord : scans
  Child ||--o{ Payment : pays
  PricePlan ||--o{ Child : assigned
  PricePlan ||--o{ Payment : basedOn
  Staff ||--o{ AttendanceRecord : uploads
  Staff ||--o{ SalaryPayment : earns
  ExpenseCategory ||--o{ Expense : groups
```

**Core entities:** User/Staff, Role/Permission, AuditLog, Class, Child (QR/barcode), EntryRecord, PricePlan, Payment, Expense/ExpenseCategory, AttendanceRecord, SalaryPayment

## Feature modules
- Bilingual UI (AR default + EN), RBAC, Audit logs
- Children + QR/Barcode entry, plans/payments, expenses, classes
- Attendance CSV upload → salary calculation
- Dashboard

## Implementation order (on approve / Agent mode)
1. Write `docs/PLAN.md` from this plan
2. Git init + remote `https://github.com/ismarteg/Nursery-Management.git`
3. Scaffold `backend/NurseryManagement.Api` + `frontend/nursery-app`
4. Local SSL docs + HTTPS launch config
5. EF provider switch (Sqlite default)
6. i18n, RBAC, audit, then domain features in order

## Out of scope (v1)
- Parent portal, payment gateways, biometrics
- Editing/deleting audit records
- Live SQL Server deploy (readiness only)
- Automated SQLite→SQL Server data migrator
