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
COPY src/Api/Hris.Api.csproj src/Api/
COPY src/BuildingBlocks/Hris.SharedKernel/Hris.SharedKernel.csproj src/BuildingBlocks/Hris.SharedKernel/
COPY src/BuildingBlocks/Hris.Foundation.Configuration/Hris.Foundation.Configuration.csproj src/BuildingBlocks/Hris.Foundation.Configuration/
COPY src/BuildingBlocks/Hris.Foundation.Logging/Hris.Foundation.Logging.csproj src/BuildingBlocks/Hris.Foundation.Logging/
COPY src/BuildingBlocks/Hris.Foundation.Identity/Hris.Foundation.Identity.csproj src/BuildingBlocks/Hris.Foundation.Identity/
COPY src/BuildingBlocks/Hris.Foundation.Events/Hris.Foundation.Events.csproj src/BuildingBlocks/Hris.Foundation.Events/
COPY src/BuildingBlocks/Hris.Foundation.Authorization/Hris.Foundation.Authorization.csproj src/BuildingBlocks/Hris.Foundation.Authorization/
COPY src/BuildingBlocks/Hris.Foundation.Audit/Hris.Foundation.Audit.csproj src/BuildingBlocks/Hris.Foundation.Audit/
COPY src/BuildingBlocks/Hris.Foundation.RulesEngine/Hris.Foundation.RulesEngine.csproj src/BuildingBlocks/Hris.Foundation.RulesEngine/
COPY src/BuildingBlocks/Hris.Foundation.Validation/Hris.Foundation.Validation.csproj src/BuildingBlocks/Hris.Foundation.Validation/
COPY src/BuildingBlocks/Hris.Foundation.Localization/Hris.Foundation.Localization.csproj src/BuildingBlocks/Hris.Foundation.Localization/
# TODO: add a COPY line per module .csproj here as each module's own Sprint adds it.

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
