# Modules

This folder holds one project group per business module, added **only when that
module's own Sprint begins**, per `IMPLEMENTATION-PLAN.md`. It is empty right now
by design, not by oversight — Phase 1 (Foundation Platform, currently in progress)
builds no business module at all; the Core Kernel and the frameworks that depend on
it come first, because every module depends on them.

Do not create a module folder here ahead of its own Sprint, even if that module's
`docs/04-modules/<module>/` specification looks more complete or more interesting
than whatever Sprint is actually current — the project's own "Coding Phase" convention
exists specifically to head this off.

## Build order, once Phase 2 begins

Reproduced from `IMPLEMENTATION-PLAN.md` so the order is visible without opening a
second file; that document remains the authoritative source if the two ever
diverge.

| Phase | Sprint | Module(s) |
|---|---|---|
| 2 — Core HR | 1 | `organization` |
| 2 — Core HR | 2 | `position` |
| 2 — Core HR | 3 | `employment` |
| 2 — Core HR | 4 | `employee` |
| 3 — Workforce Management | 1 | `administration` |
| 3 — Workforce Management | 2 | `workflow` |
| 3 — Workforce Management | 3 | `timekeeping` |
| 3 — Workforce Management | 4 | `attendance` |
| 3 — Workforce Management | 5 | `leave` |
| 4 — Payroll | 1 | `payroll` |
| 4 — Payroll | 2 | `compensation` |
| 4 — Payroll | 3 | `benefits` |
| 5 — Talent Management | 1 | `recruitment`, `performance`, `learning` (parallel — no ordering dependency among the three) |
| 5 — Talent Management | (later) | `succession`, `onboarding` (see `IMPLEMENTATION-PLAN.md` for the exact Sprint) |
| 6 — Analytics | 1 | `analytics-reporting` |
| 6 — Analytics | 2 | `workforce-planning` |

Phases 7–9 (AI, Marketplace, Global) are explicitly unspecified in
`IMPLEMENTATION-PLAN.md` — no module folder should be created against them until a
real specification exists; inventing one here would be exactly the kind of
manufactured decision point this project's engineering conventions warn against.

## Expected shape, per module (once its Sprint starts)

Each module project group should mirror `docs/08-devops/coding-standards.md`'s
layer conventions and `docs/02-architecture/02-solution-architecture/solution-overview.md`'s
Clean Architecture layering — typically four projects per module
(`Hris.Modules.<Module>.Domain`, `.Application`, `.Infrastructure`,
`.Presentation`, the last usually just API controllers registered into
`Hris.Api` rather than a separately hosted project, since ADR-0001 and
`containerization.md` both commit to one deployable artifact). `docs/04-modules/administration/`
is the reference implementation for the specification depth to build against —
consult it first when starting Phase 3 Sprint 1.
