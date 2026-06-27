# Architecture Overview

## Monorepo Goal

This platform is a modular monolith for tax compliance operations. Deployment stays simple while preserving clear boundaries between domain logic, application workflows, infrastructure concerns, and HTTP delivery.

## Core Flows (Implemented)

1. Admin or Compliance Manager defines organizations, legal entities, jurisdictions, templates, and recurring task rules.
2. A background service generates task occurrences based on recurrence settings.
3. Contributors update assigned work items and upload supporting documents.
4. Important changes write audit log entries visible per task and in the global audit log.
5. Scheduled notifications are published through RabbitMQ; production uses SMTP email delivery (Development logs messages instead).
6. Dashboard endpoints aggregate current workload, cache summary results in Redis, and expose CSV export.
7. Admins invite users and assign roles through the user management API and UI.
8. Admins switch organization context through the shell; scoped API calls include `X-Organization-Id`.

## Operational Endpoints

- `GET /health` — Postgres, Redis, and RabbitMQ health checks when connection strings and messaging are configured
- `GET /api/dashboard/summary` — workload summary with jurisdiction and legal-entity breakdowns
- `GET /api/dashboard/export` — CSV compliance status export
- `GET /api/audit-log` — paginated global audit history
- `GET/POST/PUT /api/users` — admin-only user management

## Production Notes

- Configure `Jwt:SigningKey`, seed credentials, and notification defaults through environment variables or a secret manager.
- Use `appsettings.Production.json` as a baseline and override secrets at deploy time.
- Persist Data Protection keys with `DataProtection:KeysPath` (mounted volume in production Docker) so MFA secrets survive API restarts.
- Set `OTEL_EXPORTER_OTLP_ENDPOINT` or `OpenTelemetry:OtlpEndpoint` to export traces to an OTLP collector; Development falls back to console export when unset.
- Security headers are applied by API middleware and the nginx frontend; HSTS is emitted when TLS is terminated in front of the container (`X-Forwarded-Proto: https`).
- `AuthorizationDemoController` is available only in Development.
- Uploaded files are constrained by size and allowed extensions in `FileStorage` settings.

## Why This Shape Is Beginner Friendly

- The codebase is split by purpose, not by many deployable services.
- Interfaces expose extension points without heavy abstraction everywhere.
- The frontend uses standalone Angular components with a shallow folder structure.
- Infrastructure dependencies are isolated from the domain model.

## Post-Pilot Scope

Navigation polish, Contributor UX enhancements beyond the shipped baseline, chart-based reporting, click-through drill-down from dashboard breakdowns, shared-component cleanup, and support-access patterns are tracked in [post-launch-backlog.md](post-launch-backlog.md). Organization context switching, CSV export, MFA auth UX, and contributor default routing are already shipped.
