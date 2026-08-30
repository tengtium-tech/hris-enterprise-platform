# backend/

.NET 9 modular monolith backend for the HRIS Enterprise Platform.

## Solution structure

* `Hris.sln` — the solution. `Directory.Build.props` sets solution-wide conventions: nullable reference types, implicit usings, file-scoped namespaces, and analyzers treated as build errors.
* `src/Api/` — the single deployable ASP.NET Core host. Currently exposes only `/health/live` and `/health/ready`; no business endpoints yet.
* `src/BuildingBlocks/Hris.SharedKernel/` — shared building blocks used by every framework and module: the `Result` pattern for error handling, strongly-typed IDs, and base `AggregateRoot`/`Entity`/`ValueObject` types.
* `src/BuildingBlocks/Hris.Application/` — shared Application-layer building blocks used by every framework and module: `ICommand`/`IQuery` MediatR markers, the `IUnitOfWork` abstraction, and the `ValidationBehavior`/`TransactionBehavior` MediatR pipeline behaviors, per `docs/02-architecture/03-application-architecture/application-pipeline.md`. Has no reference to Entity Framework Core or any other Infrastructure concern.
* `src/BuildingBlocks/Hris.Infrastructure/` — the one shared `HrisDbContext`, per `docs/02-architecture/05-data-architecture/dbcontext-design.md`'s "one application DbContext during the Modular Monolith phase." Discovers every framework's/module's own `IEntityTypeConfiguration` classes by assembly scanning rather than referencing them directly; implements `Hris.Application`'s `IUnitOfWork`.
* `src/BuildingBlocks/Hris.Foundation.*/` — the nine Core Kernel foundation frameworks: Configuration, Logging, Identity, Events, Authorization, Audit, Rules Engine, Validation, and Localization. Domain layer (aggregates, value objects, domain events) is implemented for all nine. Configuration Framework additionally has real Application (MediatR commands/queries, FluentValidation validators) and Infrastructure (EF Core persistence) layers; the other eight have Domain only.
* `src/Modules/` — empty by design. Business modules (organization, employee, payroll, and the rest) are added one at a time, each when its own development phase begins. See `src/Modules/README.md` for the intended build order.
* `tests/Hris.CriticalRequirements.Tests/` — one stub test per Critical Test Requirement (59 total), covering authorization, tenant isolation, audit trails, payroll correctness, workflow orchestration, and notification delivery. A test is unskipped only once the module or framework it exercises actually exists and the test genuinely exercises it.
* `Dockerfile`, `docker-compose.yml` — one backend image (multi-stage build, non-root runtime user); `docker-compose.yml` is for local Postgres/Redis only, not a deployment definition.
* `../.github/workflows/` — CI pipelines. A few steps (vulnerability scanning, container registry push, OpenAPI generation, deployment) are explicit placeholders until the corresponding tooling is chosen.
* `../.editorconfig` — formatting rules only; nullable/implicit-usings/file-scoped-namespace enforcement lives in `Directory.Build.props`.

## Current status

The Core Kernel domain layer is implemented across all nine foundation frameworks. Configuration Framework (first in Sprint 3's bootstrap order) additionally has a real Application layer (8 MediatR commands covering its full lifecycle, 2 queries, FluentValidation validators) and Infrastructure layer (EF Core Fluent API configuration, repository implementation) behind it, plus the shared `Hris.Application`/`Hris.Infrastructure` projects every subsequent framework's own Application/Infrastructure layers will reuse. The remaining eight foundation frameworks (Logging, Identity, Events, Authorization, Audit, Rules Engine, Validation, Localization) have Domain layer only. No business module exists yet — the Core Kernel has to be complete first, since every module depends on it.

## What hasn't been verified yet

* No `dotnet build`/`dotnet test` has been run against this solution in a real .NET environment. Run `dotnet build backend/Hris.sln` and `dotnet test backend/Hris.sln` as the first real check — nothing here should be assumed to compile just because it parses. This matters more than usual for `Hris.Foundation.Configuration`'s new Infrastructure layer specifically: its EF Core mapping (an owned collection over a private backing field, a Value Object compared via a custom `==` operator in LINQ queries) uses patterns that are correct per EF Core's own documentation but have not been compiled or run against a real PostgreSQL instance — see `ConfigurationSettingRepository`'s own remarks.
* `EFCore.NamingConventions` (used for the platform's one physical table/column-naming convention, per `docs/02-architecture/05-data-architecture/naming-conventions.md`'s own finding) is not yet listed in `docs/02-architecture/02-solution-architecture/technology-stack.md`'s Approved NuGet Packages — a documentation gap to close, not a silently-made substitution.
* No EF Core migration has been generated yet for `HrisDbContext` — the first `dotnet ef migrations add` run is also unverified.
* Branch protection on `main` (required status checks, required review count, no direct pushes) is a GitHub repository setting, not something a workflow file can express — configure it directly in the repo's branch protection rules.
* CI stages that need infrastructure not yet chosen (a container registry, a vulnerability scanner, a deployment target) remain placeholders in the workflow files until those decisions are made.

## Running locally

```
docker compose -f backend/docker-compose.yml up -d   # Postgres + Redis
dotnet user-secrets set "ConnectionStrings:HrisDatabase" "Host=localhost;Database=hris;Username=hris;Password=hris" --project backend/src/Api
dotnet run --project backend/src/Api
```

The connection string is required, not optional — `AddHrisInfrastructure` throws on startup if `ConnectionStrings:HrisDatabase` is missing, per module-registration.md's "fail startup loudly where a module's required configuration or dependency is absent" and environment-strategy.md's own rule that a connection string never lives in `appsettings.json`. `dotnet user-secrets` (not a checked-in file) is the right place for it locally; a real environment reads it from that environment's own secrets manager instead.

No EF Core migration exists yet, so the database itself has no schema even once the container is running — `dotnet ef migrations add InitialCreate --project backend/src/BuildingBlocks/Hris.Infrastructure --startup-project backend/src/Api` is the next real step, unverified in this sandbox per the section above.

`GET http://localhost:8080/health/live` and `/health/ready` should both respond once the host starts; `/health/ready` now depends on the database connection (it will report unhealthy without one, by design). Nothing else is wired up yet.
