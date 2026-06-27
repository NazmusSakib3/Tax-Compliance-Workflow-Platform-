# Post-Launch Backlog

This document records UX and product-surface improvements that are **explicitly out of launch scope**. Complete launch readiness (security, production configuration, minimum auth UX, CI release gates, and pilot verification) before starting net-new backlog work.

## Why These Items Are Deferred

The platform already supports the core compliance workflow: authentication (including MFA and refresh tokens), RBAC, organization-scoped data, recurring task generation, task execution, documents, audit history, dashboard summaries with CSV export, and user administration. The remaining items below improve clarity and operator experience but do not block a safe pilot release.

Do not expand this surface area until:

- Launch-blocking security and configuration work is complete.
- Production deployment can be reproduced from a clean checkout.
- CI release gates are green and actionable.
- A pilot environment is available for real-user feedback.

## Current Baseline (Shipped for Launch)

Treat this as the launch baseline; further polish belongs in the backlog below.

| Area | Current behavior | Primary references |
|------|------------------|-------------------|
| Role-filtered navigation | Sidebar hides sections the signed-in role cannot reach; Contributors see task-focused labels where configured. | `frontend/src/app/app.component.ts`, `frontend/src/app/app.routes.ts` |
| Contributor default route | Contributors land on `/task-occurrences` after login. | `frontend/src/app/app.routes.ts`, auth guards |
| Contributor task filtering | Task occurrence lists support server-side "my tasks" filtering for Contributors. | Backend task occurrence APIs; `frontend/src/app/features/task-occurrences/` |
| Contributor copy | Task list and detail pages use Contributor-specific headings and helper text when the user is Contributor-only. | `task-occurrences-page.component.ts`, `task-occurrence-detail-page.component.ts` |
| Assignment toggle | Multi-role users can switch between all visible tasks and their assignments. | Task occurrence list UI and API filters |
| Dashboard | Summary counts, 30-day trend text, jurisdiction/entity breakdowns, and CSV export. | `frontend/src/app/features/dashboard/`, `/api/dashboard/summary`, `/api/dashboard/export` |
| Admin organization context | Admins can switch organization context; API calls include `X-Organization-Id`. | `organization-context.service.ts`, organization scoping middleware |
| Auth UX | MFA login challenge, forgot/reset password pages, account security page, refresh tokens. | `frontend/src/app/features/auth/`, `/api/auth/*` |
| Ops hardening | Persisted Data Protection keys, security headers, RabbitMQ health check, optional OTLP export. | `docs/deployment.md`, `Program.cs`, `frontend/nginx.conf` |
| Shared UI | `summary-card.component.ts` is in use; no unused table scaffold remains in `shared/components`. | `frontend/src/app/shared/components/` |

Route guards remain the source of truth for authorization. Navigation filtering is a convenience layer only.

## Deferred Work (Start After Pilot Release)

### 1. Role-based navigation polish

**Goal:** Refine navigation so each role sees only the sections they need, with consistent labels and descriptions.

**Remaining polish (not required for launch):**

- Hide read-only master-data sections from Contributors if pilot feedback shows they create noise.
- Add Viewer-specific copy where read-only affordances differ from edit-capable roles.
- Align sidebar descriptions with route-level permissions across all roles.
- Add regression tests for mixed-role sessions (for example, users with multiple roles).

### 2. Contributor UX improvements

**Goal:** Make Contributor workflows faster and clearer beyond the existing default route, "my tasks" filter, labels, and assignment toggle.

**Remaining polish (not required for launch):**

- Stronger empty states and onboarding hints for first-time Contributors.
- Mobile-friendly task detail actions for field contributors.

### 3. Reporting and dashboard expansion

**Goal:** Expand analytics after pilot feedback confirms what operators need.

**Remaining polish (not required for launch):**

- Role-specific dashboard widgets (for example, Contributor vs Compliance Manager views).
- Click-through drill-down from dashboard breakdown cards to filtered task lists.
- Trend charts and period-over-period visualizations (text trend exists today).

### 4. Shared scaffold and UI cleanup

**Goal:** Keep the frontend lean as features stabilize.

**Remaining polish (not required for launch):**

- Audit `frontend/src/app/shared/` for components that can be consolidated.
- Remove duplicate table/list patterns across feature pages if a shared abstraction proves worthwhile.
- Standardize loading, error, and empty states across CRUD screens.

The roadmap previously referenced `feature-table.component.ts`; that scaffold is not present in the repository and requires no launch action.

### 5. Admin support access patterns

**Goal:** Support break-glass or impersonation workflows if compliance reviewers require them.

**Remaining polish (not required for launch):**

- Document and implement impersonation or support-access patterns if required by compliance reviewers.

Organization context switching for multi-org admins is already shipped; only pursue impersonation after pilot customers confirm the need.

## Suggested Pickup Order After Launch

1. Gather pilot feedback on Contributor and Viewer confusion points.
2. Implement navigation and Contributor UX polish driven by that feedback.
3. Add reporting only for metrics users request repeatedly (charts, drill-down navigation).
4. Revisit shared-component cleanup once new UI patterns are stable.
5. Add support-access patterns only if compliance reviewers require them.

## Related Documentation

- [README.md](../README.md) — feature coverage, setup, tests, and CI
- [docs/deployment.md](deployment.md) — production configuration and deployment
- [docs/architecture-overview.md](architecture-overview.md) — implemented core flows
- [architecture.md](../architecture.md) — monorepo structure and shipped capabilities
