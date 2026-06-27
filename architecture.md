# Modular Monolith Architecture

## Overview

This repository is a modular monolith for a Tax Compliance Workflow Platform. The system stays in a single deployable backend application while keeping code organized into clear internal layers and feature modules.

## Backend Layers

- `Domain`
  Core business entities, enums, and domain rules.
- `Application`
  Use-case contracts, DTOs, validation boundaries, and service interfaces.
- `Infrastructure`
  EF Core persistence, Identity, Redis caching, RabbitMQ messaging, file storage, and background workers.
- `Api`
  HTTP controllers, OpenAPI/Swagger, middleware, health checks, and dependency wiring.

## Implemented Capabilities

- JWT authentication with refresh-token rotation, MFA (TOTP), password reset, login lockout, and role-based authorization (`Admin`, `ComplianceManager`, `Contributor`, `Viewer`)
- CRUD for organizations, legal entities, jurisdictions, templates, and task rules
- Recurring task occurrence generation (monthly, quarterly, yearly)
- Task occurrence workflow: assignment, status changes, comments, documents, per-task audit
- Global paginated audit log API and UI
- Admin user management API and UI with organization context switching (`X-Organization-Id`)
- Dashboard summary with Redis caching, jurisdiction/entity breakdowns, 30-day trend text, and CSV export
- RabbitMQ notification publishing with SMTP email delivery in production
- Health endpoint at `/health` (Postgres, Redis, RabbitMQ when configured)
- Persisted Data Protection keys for MFA secret encryption across API restarts
- Security headers on the API and nginx frontend; optional OTLP trace export

## Frontend Shape

The Angular app is organized by feature area:

- `core`: auth, guards, interceptors, API services, organization context
- `features`: page-level workflows (dashboard, task occurrences, auth, account security)
- `shared`: reusable UI components
- `theme`: presentation-related cross-cutting code

Contributors default to `/task-occurrences` after login. Admins can switch organization context from the shell.

## Local Development

- Infrastructure: `docker compose up -d`
- API (port 8080): `dotnet run --project backend/src/TaxCompliance.Api`
- Frontend (port 4200): `cd frontend && npm start`
- Full stack in Docker: `docker compose --profile full up -d --build`

See [README.md](README.md) for setup details and environment variables.

## Production Operations

See [docs/deployment.md](docs/deployment.md) for secrets, Docker deployment, health checks, security headers, Data Protection key persistence, and optional OpenTelemetry OTLP export.

## Post-Launch Scope

Navigation polish, Contributor UX enhancements beyond the shipped baseline, chart-based reporting, drill-down navigation, shared-component cleanup, and support-access patterns are tracked in [docs/post-launch-backlog.md](docs/post-launch-backlog.md) and are intentionally deferred until after pilot feedback.
