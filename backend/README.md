# backend/

.NET 9 modular monolith backend for the HRIS Enterprise Platform.

## Solution structure

* `Hris.sln` — the solution. `Directory.Build.props` sets solution-wide conventions: nullable reference types, implicit usings, file-scoped namespaces, and analyzers treated as build errors.
* `src/Api/` — the single deployable ASP.NET Core host. Currently exposes only `/health/live` and `/health/ready`; no business endpoints yet.
* `src/BuildingBlocks/Hris.SharedKernel/` — shared building blocks used by every framework and module: the `Result` pattern for error handling, strongly-typed IDs, and base `AggregateRoot`/`Entity`/`ValueObject` types.
* `src/BuildingBlocks/Hris.Foundation.*/` — the nine Core Kernel foundation frameworks: Configuration, Logging, Identity, Events, Authorization, Audit, Rules Engine, Validation, and Localization. Domain layer (aggregates, value objects, domain events) is implemented for all nine; application and infrastructure layers are not built yet.
* `src/Modules/` — empty by design. Business modules (organization, employee, payroll, and the rest) are added one at a time, each when its own development phase begins. See `src/Modules/README.md` for the intended build order.
* `tests/Hris.CriticalRequirements.Tests/` — one stub test per Critical Test Requirement (59 total), covering authorization, tenant isolation, audit trails, payroll correctness, workflow orchestration, and notification delivery. A test is unskipped only once the module or framework it exercises actually exists and the test genuinely exercises it.
* `Dockerfile`, `docker-compose.yml` — one backend image (multi-stage build, non-root runtime user); `docker-compose.yml` is for local Postgres/Redis only, not a deployment definition.
* `../.github/workflows/` — CI pipelines. A few steps (vulnerability scanning, container registry push, OpenAPI generation, deployment) are explicit placeholders until the corresponding tooling is chosen.
* `../.editorconfig` — formatting rules only; nullable/implicit-usings/file-scoped-namespace enforcement lives in `Directory.Build.props`.

## Current status

The Core Kernel domain layer is implemented across all nine foundation frameworks. The application layer (commands, handlers, validation pipelines) and infrastructure layer (EF Core persistence, repositories) haven't been built yet for any of them. No business module exists yet — the Core Kernel has to be complete first, since every module depends on it.

## What hasn't been verified yet

* No `dotnet build`/`dotnet test` has been run against this solution in a real .NET environment. Run `dotnet build backend/Hris.sln` and `dotnet test backend/Hris.sln` as the first real check — nothing here should be assumed to compile just because it parses.
* Branch protection on `main` (required status checks, required review count, no direct pushes) is a GitHub repository setting, not something a workflow file can express — configure it directly in the repo's branch protection rules.
* CI stages that need infrastructure not yet chosen (a container registry, a vulnerability scanner, a deployment target) remain placeholders in the workflow files until those decisions are made.

## Running locally

```
docker compose -f backend/docker-compose.yml up -d   # Postgres + Redis
dotnet run --project backend/src/Api
```

`GET http://localhost:8080/health/live` and `/health/ready` should both respond once the host starts; nothing else is wired up yet.
