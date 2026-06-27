# Continuous Integration

GitHub Actions workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

Every push to `main`/`master` and every pull request runs the jobs below. A failing job blocks merge until the issue is resolved or explicitly waived with team approval.

## Jobs

| Job | Purpose | Typical failure causes |
|-----|---------|------------------------|
| `security` | Dependency review, NuGet/npm audits, Trivy filesystem scan | New high/critical CVEs, committed secrets, vulnerable packages |
| `backend` | Release build, API publish, unit/integration tests, coverage gate | Compile errors, failing tests, line coverage below 45% |
| `frontend` | Production Angular build, Karma tests, coverage gate | Build errors, failing component tests, coverage below thresholds |
| `docker` | API and frontend image builds plus Trivy image scans | Dockerfile errors, base-image vulnerabilities |
| `e2e` | Playwright smoke tests against live API + Angular dev server | Auth/UI regressions, API startup issues, flaky infrastructure waits |

## Security gate (`security`)

- **Dependency review** (pull requests only): fails when a changed dependency introduces a vulnerability rated high or critical.
- **`dotnet list package --vulnerable`**: fails when the solution references a known high or critical vulnerable NuGet package (including transitive dependencies).
- **`npm audit`**: runs in `e2e/` at high severity and in `frontend/` for production dependencies at critical severity. Angular 18 advisory noise in dev tooling is covered by Trivy and dependency review instead.
- **Trivy filesystem scan**: checks the repository for vulnerabilities, secrets, and misconfigurations. SARIF output is uploaded to GitHub Security for review.

Remediation:
1. Upgrade the affected package to a patched version.
2. If no fix exists yet, document the risk and add a time-bound exception in team tracking before merging.

## Production build verification

### Backend

`dotnet publish` runs in Release mode after `dotnet build`. The published API artifact is uploaded as `api-publish` for inspection.

Local reproduction:

```bash
dotnet build backend/TaxCompliance.sln --configuration Release -warnaserror
dotnet publish backend/src/TaxCompliance.Api/TaxCompliance.Api.csproj --configuration Release --no-build -o ./artifacts/api
```

### Frontend

`npm run build:production` replaces `environment.ts` with `environment.production.ts` and emits hashed bundles under `frontend/dist/`.

Local reproduction:

```bash
cd frontend
npm ci
npm run build:production
```

### Docker images

The `docker` job builds both container images exactly as defined in deployment compose files and scans them with Trivy.

Local reproduction:

```bash
docker build -f backend/src/TaxCompliance.Api/Dockerfile -t taxcompliance-api:local .
docker build -f frontend/Dockerfile -t taxcompliance-frontend:local .
```

## End-to-end smoke tests (`e2e`)

Playwright boots Postgres, Redis, RabbitMQ, the API, and the Angular dev server, then runs:

- `e2e/tests/auth-smoke.spec.ts` — sign-in dashboard load and forgot-password entry point
- `e2e/tests/smoke.spec.ts` — representative compliance task workflow via API + task list UI
- `e2e/tests/dashboard-smoke.spec.ts` — dashboard CSV export from the UI

Environment variables:

| Variable | Default in CI |
|----------|---------------|
| `E2E_BASE_URL` | `http://localhost:4200` |
| `E2E_API_BASE_URL` | `http://localhost:8080/api` |

Local reproduction:

```bash
docker compose up -d postgres redis rabbitmq
# start API and frontend in separate terminals
cd e2e
npm ci
npx playwright install chromium
npm test
```

On failure, download the `playwright-artifacts` workflow artifact for traces, screenshots, `api.log`, and `frontend.log`.

## Coverage gates

| Area | Threshold |
|------|-----------|
| Backend line coverage | 45% (solution total via Coverlet) |
| Frontend statements | 50% |
| Frontend branches | 35% |
| Frontend functions | 45% |
| Frontend lines | 50% |

Coverage artifacts (`backend-coverage`, `frontend-coverage`) are uploaded on every run for trend review.
