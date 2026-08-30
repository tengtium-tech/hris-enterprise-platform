# backend/

Read the project's engineering conventions (Coding Phase section) before writing anything here. The
short version: work only the Sprint `../IMPLEMENTATION-PLAN.md` currently has in
progress, cite the `docs/` specification and Critical Test Requirement every class
implements, and follow `../docs/08-devops/git-strategy.md` /
`coding-standards.md` / `ci-cd-pipeline.md` the same way a document in `docs/`
follows this repository's own documentation conventions.

---

## What exists right now (2026-08-23 scaffold)

Built as a Sprint 1 / Sprint 3 skeleton, not a finished kernel — every Foundation
project below is a structural stub (a `.csproj` and one placeholder class citing
its own source specification), not an implementation:

* `Hris.sln` — the solution, `Directory.Build.props` for solution-wide settings
  (nullable reference types, implicit usings, file-scoped namespaces, analyzers as
  build errors — per `docs/08-devops/coding-standards.md`).
* `src/Api/` — the single deployable ASP.NET Core host (ADR-0001,
  `docs/08-devops/containerization.md`). Currently exposes only `/health/live` and
  `/health/ready` (`docs/08-devops/monitoring-and-alerting.md`, NFR-OB-003) — no
  business endpoint exists yet.
* `src/BuildingBlocks/Hris.SharedKernel/` — the strongly typed ID convention every
  aggregate boundary uses (`docs/02-architecture/05-data-architecture/strongly-typed-id-mapping.md`).
* `src/BuildingBlocks/Hris.Foundation.*/` — one stub project per Sprint 3 Core
  Kernel framework (Configuration, Logging, Identity, Events, Authorization,
  Audit, RulesEngine, Validation, Localization), each citing its own
  `docs/03-foundation/*.md` source. **None has real behavior yet.** Implement
  each against its own document — do not invent a shape here first and reconcile
  it with the spec afterward.
* `src/Modules/README.md` — explains why no module project exists yet, and the
  build order for when Phase 2 starts.
* `tests/Hris.CriticalRequirements.Tests/` — one `[Fact(Skip = "...")]` stub per
  Critical Test Requirement (59, confirmed by direct count against
  `docs/09-testing/critical-test-requirements.md` — the project's own tracker had
  drifted to "58" and was corrected alongside this scaffold). Remove a `Skip`
  attribute only when the module or framework it exercises actually exists and the
  test actually exercises it — an unskipped stub that doesn't assert anything real
  makes the suite lie about coverage.
* `Dockerfile`, `docker-compose.yml` — per `docs/08-devops/containerization.md`
  (one backend image, multi-stage, non-root runtime user) and
  `environment-strategy.md`'s note that a local dev machine is out of that
  document's own scope. `docker-compose.yml` is for local Postgres/Redis only —
  never a Development/Staging/Production definition.
* `../.github/workflows/pr-pipeline.yml` and `main-pipeline.yml` — implement
  `docs/08-devops/ci-cd-pipeline.md`'s stated stages. Several steps are explicit
  placeholders (vulnerability scan, registry push, OpenAPI generation, Development
  deployment) because the tooling or target they need doesn't exist yet — each
  says so in its own comment and states what unblocks it.
* `../.editorconfig` — formatting only, per `coding-standards.md`'s own scoping;
  nullable/implicit-usings/file-scoped-namespace enforcement lives in
  `Directory.Build.props` instead.

## What this scaffold deliberately does not do

* No module (`organization`, `administration`, `payroll`, ...) has a project yet.
  See `src/Modules/README.md`.
* No Foundation framework has real behavior. Every kernel project is a stub.
* No CI stage that needs infrastructure not yet decided (a registry, a
  vulnerability scanner, a Development deployment target) is implemented for
  real — each is a clearly marked placeholder in the workflow files.
* **No `dotnet build`/`dotnet test` has been run against any of this.** The
  environment this scaffold was built in has no .NET SDK and restricted network
  access, so nothing here has been compiled. Run `dotnet build backend/Hris.sln`
  and `dotnet test backend/Hris.sln` as the first real verification step, in a
  normal development machine or CI runner — do not assume this compiles because
  it parses.
* Branch protection on `main` (required status checks, required review count,
  no direct pushes — `docs/08-devops/git-strategy.md`) is a GitHub repository
  setting, not something expressible in a workflow YAML file. Configure it once
  this repository has a real GitHub remote; the workflow files above only supply
  the status checks that setting would require to pass.

## Running locally, once the above is verified to build

```
docker compose -f backend/docker-compose.yml up -d   # Postgres + Redis
dotnet run --project backend/src/Api
```

`GET http://localhost:8080/health/live` and `/health/ready` should both respond
once the host starts; nothing else is wired up yet.

## Where to look next

`../IMPLEMENTATION-PLAN.md` for the current Sprint. The project's engineering
conventions for the coding discipline. `docs/04-modules/administration/` for the reference depth a module
implementation should match, once Phase 3 Sprint 1 (`administration`) begins.
