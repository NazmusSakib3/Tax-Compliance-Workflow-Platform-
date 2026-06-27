# Next Direction Decision

**Date:** 2025-06-19  
**Status:** Active  
**Chosen path:** **Pilot verification** (Approach A)

## Context

The platform is past feature-complete MVP. Launch-blocking security, production configuration, minimum auth UX, and CI release gates are in place. The remaining fork is whether to deploy for real-user feedback now, harden operations first, or stabilize docs/tests before moving forward.

## Options Considered

| Approach | Summary | Fit for current state |
|----------|---------|------------------------|
| **A — Pilot verification** | Run `scripts/pilot-release.ps1` or production compose; verify health, login, reset, MFA, export; rotate admin password; gather feedback | **Selected** — aligns with Phase 6 in the launch roadmap |
| **B — Ops hardening** | Persisted Data Protection keys, security headers, RabbitMQ health check, OTLP export, optional CD workflow | Defer unless pilot is internet-facing or multi-tenant |
| **C — Docs/test stabilization** | Sync backlog/architecture docs; add auth refresh/MFA integration tests; dashboard export E2E | Run in parallel or immediately after pilot, not as a gate |

## Baseline Verification (2025-06-19)

Local checks before choosing:

| Check | Result |
|-------|--------|
| Backend tests (`dotnet test`) | **41 passed**, 0 failed |
| Frontend tests (`npm run test:ci`) | **35 passed**, ~61% line coverage |
| CI workflow (`.github/workflows/ci.yml`) | Security scans, coverage gates, Docker builds, Playwright e2e |
| Pilot tooling | `scripts/pilot-release.ps1`, `.env.pilot.example`, `docs/deployment.md` |

## Decision Rationale

1. **Launch phases 1–5 are complete.** Security fixes (reset tokens, document auth, MFA protection, lockout), production config (CORS, email, env files), auth UX (MFA challenge, forgot/reset, account security), and CI gates are shipped.
2. **Baseline is healthy.** Backend and frontend test suites pass locally; CI is configured to enforce the same gates on merge.
3. **Pilot tooling exists.** The Windows pilot script automates local CI, deploy, health check, password-reset verification, and admin password rotation.
4. **Ops gaps are acceptable for a single-node internal pilot.** In-memory Data Protection keys, console-only OpenTelemetry, and local upload volumes are documented risks—not blockers for a controlled pilot on one host.
5. **Docs are stale but misleading, not blocking.** `post-launch-backlog.md` lists items already implemented (org switcher, contributor landing, CSV export). Syncing docs does not unblock deployment; pilot feedback will better inform what to polish next.
6. **Plan recommendation matches.** Approach A is explicitly recommended for internal/single-node pilots; ops hardening (B) is reserved for external or compliance-reviewed deployments.

## Reconsider B (ops hardening) if

- The pilot is exposed on the public internet without a managed TLS edge
- Multiple API containers will run behind a load balancer (Data Protection key persistence becomes required)
- A customer or security review mandates HSTS/CSP, centralized observability, or RabbitMQ in `/health` before any access

## Reconsider C (docs/tests) as primary if

- CI is red and cannot be fixed quickly
- A regression is discovered in auth refresh, MFA, or document download that blocks safe pilot use

## Immediate Next Steps (Pilot Verification)

1. Copy and fill secrets: `cp .env.pilot.example .env.pilot` (or let the script generate them).
2. Run pilot release:

   ```powershell
   .\scripts\pilot-release.ps1
   ```

   Use `-SkipCi` only if CI was just verified locally; use `-SkipDeploy` / `-SkipVerify` for partial reruns.

3. Manually confirm:
   - `GET /health` returns healthy Postgres and Redis
   - Login as seeded admin; rotate password on first sign-in
   - Forgot/reset password flow (MailHog in pilot profile)
   - MFA enrollment on `/account/security` and MFA login challenge
   - Dashboard CSV export (`/api/dashboard/export`)
   - One full task workflow (Compliance Manager + Contributor)
4. After pilot is running: sync `docs/post-launch-backlog.md` and `docs/architecture-overview.md` to reflect shipped features (Approach C in parallel).
5. Capture pilot feedback before picking net-new product work (charts, drill-down, mobile UX).

## Exit Criteria (Pilot Phase)

- Pilot environment reachable (HTTPS when TLS/DNS are configured for non-local pilots)
- Password reset emails arrive and complete successfully
- At least one Compliance Manager and one Contributor complete a task end-to-end
- CI release gates pass on the release branch or tag
