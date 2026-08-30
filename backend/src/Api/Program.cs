using System.Globalization;
using Hris.Application;
using Hris.Foundation.Configuration;
using Hris.Foundation.Logging;
using Hris.Infrastructure;
using Hris.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog replaces the default Microsoft.Extensions.Logging providers entirely, per
// technology-stack.md's Monitoring & Observability table naming Serilog as this
// platform's own logging technology. Bootstrapped here, before any AddXFramework()
// call, so Serilog.ILogger is already resolvable from the container -- SerilogLogSink
// (Logging Framework's own Infrastructure layer) constructor-injects it -- the
// moment AddLoggingFramework() runs below. Console is this Sprint's one configured
// sink; environment-strategy.md governs adding a production sink later without any
// framework code changing, since ILogSink's own contract does not change.
// InvariantCulture, not the host machine's current culture, formats numbers and dates
// in the rendered output template (CA1305) -- log output read by tooling/another
// operator should not silently vary with whatever locale the process happens to run
// under.
builder.Host.UseSerilog((_, loggerConfiguration) =>
    loggerConfiguration.WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// module-registration.md's Registration Flow: Program.cs -> AddFoundation() ->
// AddInfrastructure() -> business modules (none yet -- Phase 2 onward) -> Build().
// That document names AddFoundation()/AddInfrastructure() as illustrative single
// calls; this host inlines each Foundation framework's own AddXFramework() instead of
// wrapping them in one combined AddFoundation() helper, since module-registration.md's
// own Module Isolation section holds frameworks to the same "no shared registration
// file that enumerates every module" standard it states for business modules -- adding
// a combined AddFoundation() wrapper here would be exactly that shared file, one layer
// up.
//
// Order matters: AddHrisApplicationBehaviors() registers the pipeline behaviors any
// framework's MediatR pipeline runs through; AddConfigurationFramework() must run
// before AddHrisInfrastructure() so its assembly is in PersistenceAssemblyRegistry
// before HrisDbContext's model is ever built (see that registry's own remarks).
// AddLoggingFramework() runs after AddConfigurationFramework() specifically because
// LoggingService issues a MediatR query against it (see that class's own remarks) --
// DI resolution itself is order-independent, but this ordering keeps the file
// readable as "each framework's own upstream dependencies are registered before it."
// Identity, Events, Authorization, Audit, RulesEngine, Validation, and Localization
// frameworks join this list in the same bootstrap order as their own
// Application/Infrastructure layers are built, per IMPLEMENTATION-PLAN.md -- none of
// the remaining seven has one yet (backend/README.md), so only Configuration and
// Logging appear below today.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHrisApplicationBehaviors();
builder.Services.AddConfigurationFramework();
builder.Services.AddLoggingFramework();
builder.Services.AddHrisInfrastructure(builder.Configuration);

// naming-conventions.md aside: this is a "readiness" check on the connection, not a
// query against any specific table, so it stays correct regardless of which
// framework's tables exist yet.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HrisDbContext>(name: "hris-database", tags: ["ready"]);

var app = builder.Build();

// logging-framework.md's own Log Categories section names "API Logs" (HTTP Method,
// Endpoint, Status Code, Duration) as one of the framework's five log categories;
// Serilog.AspNetCore's request-logging middleware is the concrete implementation of
// exactly that category, emitted through the same Serilog pipeline
// UseSerilog() configured above -- not a second, parallel logging mechanism.
app.UseSerilogRequestLogging();

// docs/08-devops/monitoring-and-alerting.md, "Health Monitoring (NFR-OB-003)":
// liveness and readiness are distinct endpoints with distinct check sets, never
// merged into one.
//
// /health/live  -- "is the process itself still functioning" -- no external
//                  dependency checks. The container host restarts on failure here
//                  (docs/08-devops/containerization.md). Predicate excludes every
//                  registered check (the "ready"-tagged database check included), per
//                  that same distinction -- liveness never depends on a downstream
//                  connection.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

// /health/ready -- "should this instance currently receive traffic" -- includes the
// "ready"-tagged HrisDbContext connectivity check registered above. Cache (Redis)
// connectivity joins this same predicate once Redis is wired up in a later Sprint.
// ci-cd-pipeline.md's post-deployment "Health check verification" step calls this
// endpoint, not /health/live -- an instance that is alive but still warming its
// connection pool should not be judged a failed deployment.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.Run();
