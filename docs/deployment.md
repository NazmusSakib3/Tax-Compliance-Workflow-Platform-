# Production Deployment

This guide covers the production-facing configuration for the tax compliance platform. Keep real values in your deployment platform, secret manager, or an uncommitted environment file.

## Clean Checkout Deployment

1. Copy the production environment checklist:

```bash
cp .env.production.example .env.production
```

2. Replace every placeholder in `.env.production` with real secrets and URLs. Do not commit that file.

3. Build and start the production stack:

```bash
docker compose --env-file .env.production -f docker-compose.production.yml up -d --build
```

4. Verify the deployment:

```bash
curl -f http://localhost/health
```

Use your public HTTPS URL after TLS and DNS are configured.

5. Sign in with the seeded administrator, rotate the admin password, and confirm password-reset email delivery if that flow is enabled.

### Local pilot / staging

For a production-like pilot on your machine (MailHog captures outbound email):

```bash
cp .env.pilot.example .env.pilot
# edit .env.pilot if needed, or let the script generate secrets
```

Windows PowerShell:

```powershell
.\scripts\pilot-release.ps1
```

The script runs local CI checks, deploys with `docker-compose.production.yml` plus `docker-compose.pilot.yml`, verifies `/health`, exercises forgot/reset password through MailHog, and rotates the seeded admin password. A `pilot-release-report.txt` file is written locally with the rotated credentials (gitignored).

Skip flags: `-SkipCi`, `-SkipDeploy`, `-SkipVerify`. Use `-NoCache` to rebuild images without Docker build cache and re-pull base images (see [Docker troubleshooting](#docker-troubleshooting) below).

For local development, keep using:

```bash
docker compose up -d
docker compose --profile full up -d --build
```

The default compose file remains development-oriented and keeps localhost ports open for debugging.

## Required Secrets

Use `.env.production.example` as the checklist. These values are required for the Docker production profile and should be managed as secrets:

| Variable | Purpose |
|----------|---------|
| `Jwt__SigningKey` | JWT signing secret, at least 32 random characters |
| `Seed__AdminEmail` / `Seed__AdminPassword` | Initial administrator bootstrap credentials |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `REDIS_PASSWORD` | Redis password |
| `RABBITMQ_DEFAULT_PASS` | RabbitMQ password |
| `Email__Password` and related SMTP settings | Outbound email delivery |
| `Notifications__DefaultRecipientEmail` | Fallback notification recipient |
| `PUBLIC_APP_ORIGIN` | Public browser origin for CORS |
| `PasswordReset__ClientResetUrl` | Password reset page URL, for example `https://tax-compliance.example.com/reset-password` |
| `NG_APP_API_BASE_URL` | Frontend API base URL baked into the production Angular build |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Optional OTLP collector endpoint for distributed tracing |

Rotate the initial admin password after the first production sign-in. Do not commit `.env`, `.env.production`, provider export files, or copied secret values.

## Frontend And API URLs

Development builds use `frontend/src/environments/environment.ts` and call `http://localhost:8080/api`.

Production builds use `frontend/src/environments/environment.production.ts`. By default that file points to `/api`, which works with the production nginx container because nginx proxies `/api` and `/health` to the API service on the same public origin.

To build the frontend for a different API location, set `NG_APP_API_BASE_URL` before the production build:

```bash
cd frontend
set NG_APP_API_BASE_URL=https://api.example.com/api   # Windows CMD
# export NG_APP_API_BASE_URL=https://api.example.com/api  # bash
npm run build:production
```

The production Docker image runs `frontend/scripts/write-production-environment.js` during the image build and honors the `NG_APP_API_BASE_URL` build argument. The default Docker value is `/api`.

If the frontend and API are deployed on different origins, also set `Cors__AllowedOrigins__0` or `Cors__AllowedOrigins` on the API to the public frontend origin, for example `https://tax-compliance.example.com`. Do not use wildcard CORS in production.

## Email Delivery

Development uses the `Development` email provider, which logs messages instead of sending them.

Production uses the `Smtp` provider through `SmtpEmailSender`. Configure these API settings:

| Variable | Purpose |
|----------|---------|
| `Email__Provider` | Set to `Smtp` in production |
| `Email__FromAddress` | Sender email address |
| `Email__FromName` | Sender display name |
| `Email__SmtpHost` | SMTP server hostname |
| `Email__SmtpPort` | SMTP port, default `587` |
| `Email__EnableSsl` | TLS setting, default `true` |
| `Email__Username` / `Email__Password` | SMTP credentials when required by the provider |

The API validates SMTP settings at startup outside Development.

## CORS

CORS origins are configured through `Cors:AllowedOrigins` in configuration or environment variables such as:

```bash
Cors__AllowedOrigins__0=https://tax-compliance.example.com
```

Comma-separated values are also supported through `Cors__AllowedOrigins`.

In Development only, the API falls back to `http://localhost:4200` when no origins are configured. Production requires at least one explicit origin.

## Docker Deployment

The production compose file:

- Runs the API with `ASPNETCORE_ENVIRONMENT=Production`.
- Requires explicit database, Redis, RabbitMQ, JWT, seed admin, notification, CORS, SMTP, and password-reset values.
- Serves the frontend as static files through nginx instead of `ng serve`.
- Exposes only the frontend HTTP port by default.
- Persists Postgres, Redis, RabbitMQ, uploaded files, and Data Protection keys in named volumes.
- Waits for the API health check before starting the frontend container.

Container images:

- `backend/src/TaxCompliance.Api/Dockerfile` publishes the .NET API on port `8080`.
- `frontend/Dockerfile` builds the Angular app and serves it with nginx on port `80`.

## Health Checks

The API exposes `GET /health`. In production, this endpoint includes Postgres, Redis, and RabbitMQ checks when their connection strings and messaging settings are configured. The nginx frontend container proxies `/health` to the API container. The production compose file also defines an API container health check used to gate frontend startup.

## Security Headers And Data Protection

The nginx frontend adds `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, and a Content-Security-Policy suitable for the Angular SPA. When TLS terminates in front of the container and requests arrive with `X-Forwarded-Proto: https`, nginx also emits `Strict-Transport-Security`.

The API applies the same baseline headers through middleware. Set `SecurityHeaders__EnableHsts=true` in production when the API is HTTPS-facing.

MFA TOTP secrets are encrypted with ASP.NET Core Data Protection. In production Docker, keys persist to `/app/data-protection-keys` via the `data-protection-keys` volume and `DataProtection__KeysPath`. Without a persisted key ring, MFA secrets become unreadable after API container recreation.

## Observability

Development exports OpenTelemetry traces to the console. Production can export traces to any OTLP-compatible collector by setting `OTEL_EXPORTER_OTLP_ENDPOINT` (for example `http://otel-collector:4317`). When no OTLP endpoint is configured in Production, trace export is disabled to avoid noisy console output.

## File Storage

The current production compose file persists uploads with the `upload-storage` Docker volume mounted at `/app/storage/uploads`. Local file storage is acceptable for an MVP pilot on a single node. Replace it with object storage before scaling horizontally or when long-term retention and durability requirements exceed local disk guarantees.

## Operational Notes

- Terminate TLS at a reverse proxy, load balancer, or platform edge in front of the frontend container.
- Set `AllowedHosts` to the API hostnames if the API is exposed directly.
- Keep Swagger disabled in Production unless a protected documentation route is added.
- Run database migrations during API startup only when the deployment process allows startup-time migrations.
- Run the backend and frontend test suites before promoting a release.

## Docker Troubleshooting

### Empty `runtimeconfig.json` during API image build

If `docker compose build` fails during `dotnet restore` inside the API container with an error like:

```
A JSON parsing exception occurred in [/usr/share/dotnet/sdk/8.0.422/dotnet.runtimeconfig.json] ... The document is empty.
Invalid runtimeconfig.json [...]
```

the failure is in the **base .NET SDK image layer**, not in application source. Local `dotnet restore` and CI may still succeed; only Docker Desktop on the affected machine is corrupted.

**Recovery steps (run in order):**

1. Restart Docker Desktop (Troubleshoot → Restart).

2. Clear the corrupted build cache:

```powershell
docker builder prune -af
```

3. Re-pull base images without cache:

```powershell
docker pull mcr.microsoft.com/dotnet/sdk:8.0
docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker pull node:20-alpine
docker pull nginx:1.27-alpine
```

4. Rebuild the pilot stack without cache:

```powershell
docker compose --env-file .env.pilot -f docker-compose.production.yml -f docker-compose.pilot.yml build --no-cache
docker compose --env-file .env.pilot -f docker-compose.production.yml -f docker-compose.pilot.yml up -d
```

Or use the pilot release script:

```powershell
.\scripts\pilot-release.ps1 -SkipCi -NoCache
```

5. Verify the API is healthy:

```powershell
Invoke-WebRequest http://localhost:8088/health -UseBasicParsing
```

If step 3 still shows an empty `runtimeconfig.json`, use **Docker Desktop → Troubleshoot → Clean / Purge data**, then repeat from step 2.

**Workaround:** run infrastructure in Docker (Postgres, Redis, RabbitMQ) and start the API and frontend locally. See the hybrid pilot sprint plan for details.

The API Dockerfile runs `dotnet --info` before `dotnet restore` so a corrupted SDK layer fails fast with a clear error instead of an obscure JSON parse failure mid-restore.
