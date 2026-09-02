# syntax=docker/dockerfile:1-labs
# Grounded in docs/08-devops/containerization.md's "Image Strategy" (id: cont01):
# one application image, multi-stage build, non-root runtime user, .NET 9
# ASP.NET Core *runtime* image for Stage 2 (never the SDK image -- "smaller attack
# surface and smaller image size, since the runtime stage never needs a compiler").
#
# This is the ONE backend image -- containerization.md is explicit that a Dockerfile
# per business module is the wrong shape ADR-0001 already rejected at the
# service-architecture layer; every module compiles into this same image once its
# own Sprint adds it as a ProjectReference of Hris.Api.

# ---- Stage 1: build ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Build.props .
COPY Hris.sln .
# Every .csproj under src/, discovered by glob rather than named by hand -- this
# used to be a per-project COPY list, and forgetting to add a new line to it broke
# the container build twice for the identical reason (HEP-43: Hris.Application/
# Hris.Infrastructure missing when they were introduced; HEP-54:
# Hris.Foundation.Tenant missing when Sprint 4 introduced it). Neither `dotnet
# build`/`test`/`format` -- this repo's own standard pre-commit checks -- ever
# touches the Dockerfile, so a missing line had no local signal either time; a
# hand-maintained list was always going to keep breaking the same way through the
# rest of Sprint 4, Sprints 5-9, and eventually nineteen business modules under
# src/Modules/.
#
# `--parents` (Dockerfile syntax 1-labs; stable `:1`/`:1.7` do not yet carry this
# flag, confirmed by testing both before choosing `1-labs`) preserves each matched
# file's own directory path under /src -- a plain wildcard COPY instead flattens
# every match into one destination directory, which `dotnet restore` cannot resolve
# against `Hris.sln`'s own project paths. `src/Modules/*/*.csproj` matches nothing
# today (`src/Modules/` is empty by design, per this project's own backend/README.md,
# until Phase 2 adds the first business module) -- confirmed by testing that a
# zero-match glob does not fail this COPY as long as at least one glob in the same
# command matches something, which `src/BuildingBlocks/*/*.csproj` always does.
COPY --parents src/Api/*.csproj src/BuildingBlocks/*/*.csproj src/Modules/*/*.csproj ./

RUN dotnet restore src/Api/Hris.Api.csproj

COPY . .
RUN dotnet publish src/Api/Hris.Api.csproj -c Release -o /app/publish --no-restore

# ---- Stage 2: runtime ----
# ASP.NET Core runtime image, not the SDK image (containerization.md, Image Strategy).
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# containerization.md, Image Security: "The application process inside the
# container runs as a non-root user."
RUN adduser --disabled-password --gecos "" hrisapp
USER hrisapp

COPY --from=build /app/publish .

# containerization.md, Image Strategy: "Expose the application port and the
# health-check port." Using one shared port here (8080) for both application
# traffic and the /health/live, /health/ready endpoints Program.cs maps --
# monitoring-and-alerting.md does not mandate a distinct health-check port number,
# only distinct health-check *paths*; revisit this if that changes.
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Hris.Api.dll"]
