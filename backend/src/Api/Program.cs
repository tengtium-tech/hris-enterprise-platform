using Hris.Application;
using Hris.Foundation.Configuration;
using Hris.Infrastructure;
using Hris.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

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
// Logging, Identity, Events, Authorization, Audit, RulesEngine, Validation, and
// Localization frameworks are registered here in the same bootstrap order as their own
// Application/Infrastructure layers are built, per IMPLEMENTATION-PLAN.md -- none of
// the other eight has one yet (backend/README.md), so only Configuration appears below
// today.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHrisApplicationBehaviors();
builder.Services.AddConfigurationFramework();
builder.Services.AddHrisInfrastructure(builder.Configuration);

// naming-conventions.md aside: this is a "readiness" check on the connection, not a
// query against any specific table, so it stays correct regardless of which
// framework's tables exist yet.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HrisDbContext>(name: "hris-database", tags: ["ready"]);

var app = builder.Build();

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
