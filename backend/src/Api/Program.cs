using System.Globalization;
using Hris.Application;
using Hris.Foundation.Audit;
using Hris.Foundation.Authorization;
using Hris.Foundation.Configuration;
using Hris.Foundation.Events;
using Hris.Foundation.Identity;
using Hris.Foundation.Logging;
using Hris.Foundation.RulesEngine;
using Hris.Foundation.Validation;
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
// Localization Framework joins this list once its own Application/Infrastructure
// layers are built, per IMPLEMENTATION-PLAN.md -- it does not have one yet
// (backend/README.md), so Configuration, Logging, Identity, Event, Authorization,
// Audit, RulesEngine, and now Validation appear below. AddEventFramework()
// runs after AddIdentityFramework() and AddConfigurationFramework() for the same
// reason AddIdentityFramework() itself does: OutboxDispatcherBackgroundService issues
// a MediatR query against Configuration Framework, and EventEnvelope's own Actor field
// is a Hris.Foundation.Identity type (see those classes' own remarks).
// AddAuthorizationFramework() runs after AddIdentityFramework() for the same reference
// reason (RoleAssignment.PrincipalId is a Hris.Foundation.Identity type).
// AddAuditFramework() runs after both: its own AuditRecorder publishes through Event
// Framework's own IEventPublisher, and its own SearchAuditRecordsQueryHandler/
// GetAuditRecordByIdQueryHandler issue a MediatR query against both Authorization
// Framework and Configuration Framework. AddRulesEngineFramework() runs last of the
// seven: every rule-management command issues a MediatR query against Authorization
// Framework's own CheckAuthorizationQuery, per rules-engine.md's own "Only authorized
// users should publish or modify business rules" -- unlike Audit Framework,
// RulesEngine deliberately does not publish through Event Framework for its own
// RuleCreated/RulePublished/RuleDeprecated/RuleArchived domain events (see
// CreateRuleDefinitionCommand's own remarks: doing so would need a tenant-context
// field this Domain layer's own event records do not carry, the same class of gap
// already deferred elsewhere in this Sprint), so it does not need to run after
// AddEventFramework() for that reason -- only after AddAuthorizationFramework().
// AddValidationFramework() runs after AddConfigurationFramework(), the only one of
// its own five Upstream Dependencies (Rules Engine, Configuration, Logging,
// Localization, Audit) it actually calls through MediatR today -- ValidationService
// resolves its own ValidationPolicy through it, the identical integration
// LoggingService's own remarks establish for its sibling minimum-severity lookup.
// Localization Framework, also listed as an Upstream Dependency, is not built yet
// (IMPLEMENTATION-PLAN.md's own bootstrap order places it last of the nine); nothing
// in this framework's Application layer references it -- see ValidationService's own
// remarks for where that integration point will land once Localization exists.
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHrisApplicationBehaviors();
builder.Services.AddConfigurationFramework();
builder.Services.AddLoggingFramework();
builder.Services.AddIdentityFramework();
builder.Services.AddEventFramework();
builder.Services.AddAuthorizationFramework();
builder.Services.AddAuditFramework();
builder.Services.AddRulesEngineFramework();
builder.Services.AddValidationFramework();
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
