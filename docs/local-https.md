# Local HTTPS setup

Use HTTPS for both the API and Angular so browser camera/scanner features and cookies work without mixed-content issues.

## 1. Trust the ASP.NET Core development certificate

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Confirm:

```powershell
dotnet dev-certs https --check --trust
```

## 2. Run the API on HTTPS

From `backend/NurseryManagement.Api`:

```powershell
dotnet run --launch-profile https
```

Swagger: `https://localhost:7267/swagger`

## 3. Angular with SSL

Option A — Angular CLI with SSL enabled:

```powershell
cd frontend/nursery-app
ng serve --ssl
```

Option B — use exported cert files under `frontend/certs/` (gitignored):

```powershell
# Export after trusting the ASP.NET cert (example)
# Place localhost.pem / localhost-key.pem in frontend/certs/
ng serve --ssl --ssl-cert certs/localhost.pem --ssl-key certs/localhost-key.pem
```

App URL: `https://localhost:4200`

## 4. CORS

The API allows `https://localhost:4200` and `http://localhost:4200` in Development.

## Renew / fix trust issues

If the browser still warns:

```powershell
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

Then restart both servers.
