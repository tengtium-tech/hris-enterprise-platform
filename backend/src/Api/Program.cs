using System.Globalization;
using Hris.Application;
using Hris.Foundation.Audit;
using Hris.Foundation.Authorization;
using Hris.Foundation.Configuration;
using Hris.Foundation.Events;
using Hris.Foundation.Extension;
using Hris.Foundation.FileStorage;
using Hris.Foundation.Identity;
using Hris.Foundation.JobProcessing;
using Hris.Foundation.Localization;
using Hris.Foundation.Logging;
using Hris.Foundation.Numbering;
using Hris.Foundation.RulesEngine;
using Hris.Foundation.Scheduling;
using Hris.Foundation.Search;
using Hris.Foundation.Tenant;
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
// All nine Sprint 3 Core Kernel frameworks are wired below: Configuration, Logging,
// Identity, Event, Authorization, Audit, RulesEngine, Validation, and now
// Localization -- the same bootstrap order IMPLEMENTATION-PLAN.md's own dependency-
// cycle resolution states. AddEventFramework()
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
// Localization Framework, also listed as Validation's own Upstream Dependency, was
// not built yet at that point (IMPLEMENTATION-PLAN.md's own bootstrap order places
// it last of the nine); nothing in Validation's own Application layer references it
// -- see ValidationService's own remarks for where that integration point will land
// now that Localization exists. AddLocalizationFramework() itself runs after
// AddConfigurationFramework(), the one of its own three Upstream Dependencies
// (Configuration, Audit, Logging) it actually calls through MediatR --
// ResolveTranslationQuery resolves its own configurable fallback-locale chain
// through it, the identical integration ValidationService's own remarks establish
// for its sibling policy lookup. Audit Framework, also a stated Upstream Dependency,
// is deliberately not wired: IAuditRecorder.RecordAsync requires a real tenant id to
// populate the Event Framework envelope it also publishes (CTR-ISO-004), and neither
// CountryConfiguration nor TranslationEntry carries a tenant field in this Sprint's
// own built shape -- see DependencyInjection's own remarks for the full reasoning.
// With Localization now wired, all nine Sprint 3 Core Kernel frameworks are
// registered; Sprint 4's own eight frameworks each depend only on this kernel.
// AddTenantFramework() is the first of those eight (IMPLEMENTATION-PLAN.md's own
// Sprint 4 row: "no forced order among them... all eight are equally ready"), placed
// after every kernel framework it could plausibly call through MediatR and before
// AddHrisInfrastructure() for the same PersistenceAssemblyRegistry-ordering reason
// every framework above it is. Of its own five stated Upstream Dependencies
// (Identity, Authorization, Configuration, Audit, Localization), only Identity is
// concretely wired this Sprint -- see Hris.Foundation.Tenant's own
// DependencyInjection.cs for why the other four are real dependencies with no
// concrete integration point yet, not gaps.
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
builder.Services.AddLocalizationFramework();
builder.Services.AddTenantFramework();
// AddExtensionFramework() is the second of Sprint 4's own eight frameworks, no
// forced order among them. None of its own six stated Upstream Dependencies (Event,
// Validation, Authorization, Configuration, Logging, Audit) is concretely wired
// through MediatR this Sprint -- see Hris.Foundation.Extension's own
// DependencyInjection.cs for why each is a real dependency with no concrete
// integration point yet, not a gap.
builder.Services.AddExtensionFramework();
// AddFileStorageFramework() is the third of Sprint 4's own eight frameworks, no
// forced order among them. Of its own four stated Upstream Dependencies (Identity,
// Authorization, Audit, Configuration), only Identity is concretely used this Sprint
// (UserAccountId on every FileVersion) -- see DependencyInjection.cs for why the
// other three are real dependencies with no concrete integration point yet, not a gap.
builder.Services.AddFileStorageFramework();
// AddNumberingFramework() is the fourth of Sprint 4's own eight frameworks, no
// forced order among them. None of its own three stated Upstream Dependencies
// (Configuration, Audit, Authorization) is concretely wired through MediatR this
// Sprint -- see DependencyInjection.cs for why each is a real dependency with no
// concrete integration point yet, not a gap.
builder.Services.AddNumberingFramework();
// AddSearchFramework() is the fifth of Sprint 4's own eight frameworks, no forced
// order among them. Unlike its four siblings above, its own AI Implementation
// Guidance names CTR-ISO-001 explicitly ("a search index is a common isolation
// gap") -- tenant isolation is concretely wired this Sprint, not deferred; of its own
// five stated Upstream Dependencies (Event, Configuration, Authorization, Identity,
// Audit), none of the remaining four is concretely wired through MediatR yet -- see
// DependencyInjection.cs for why each is a real dependency with no concrete
// integration point yet, not a gap.
builder.Services.AddSearchFramework();
// AddSchedulingFramework() is the sixth of Sprint 4's own eight frameworks, no forced
// order among them. Like Search Framework above, its own AI Implementation Guidance
// names a tenant-isolation CTR explicitly (CTR-ISO-004, "establish explicit tenant
// context in every scheduled execution") -- tenant context is concretely wired this
// Sprint, not deferred; of its own four stated Upstream Dependencies (Configuration,
// Event, Audit, Logging), none is concretely wired through MediatR yet -- see
// DependencyInjection.cs for why each is a real dependency with no concrete
// integration point yet, not a gap.
builder.Services.AddSchedulingFramework();
// AddJobProcessingFramework() is the seventh of Sprint 4's own eight frameworks, no
// forced order among them. Like Scheduling Framework above, its own AI Implementation
// Guidance names CTR-ISO-004 explicitly, with the sharpest reason yet: "a job has no
// request to inherit context from, which is where isolation is most often lost." Of
// its own four stated Upstream Dependencies (Event, Configuration, Audit, Logging),
// none is concretely wired through MediatR yet -- see DependencyInjection.cs for why
// each is a real dependency with no concrete integration point yet, not a gap.
builder.Services.AddJobProcessingFramework();
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
