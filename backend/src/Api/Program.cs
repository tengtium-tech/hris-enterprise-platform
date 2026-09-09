using System.Globalization;
using Hris.Api.Endpoints;
using Hris.Api.Http;
using Hris.Api.Logging;
using Hris.Api.Middleware;
using Hris.Application;
using Hris.Foundation.Audit;
using Hris.Foundation.Authorization;
using Hris.Foundation.Caching;
using Hris.Foundation.Configuration;
using Hris.Foundation.DocumentManagement;
using Hris.Foundation.Entitlement;
using Hris.Foundation.Events;
using Hris.Foundation.Extension;
using Hris.Foundation.FileStorage;
using Hris.Foundation.Identity;
using Hris.Foundation.Integration;
using Hris.Modules.Administration;
using Hris.Modules.Workflow;
using Hris.Modules.Employee;
using Hris.Modules.Employment;
using Hris.Modules.Organization;
using Hris.Modules.Position;
using Hris.Foundation.JobProcessing;
using Hris.Foundation.Localization;
using Hris.Foundation.Logging;
using Hris.Foundation.Notification;
using Hris.Foundation.Numbering;
using Hris.Foundation.RulesEngine;
using Hris.Foundation.Scheduling;
using Hris.Foundation.Search;
using Hris.Foundation.StatutoryReferenceData;
using Hris.Foundation.Tenant;
using Hris.Foundation.Validation;
using Hris.Foundation.WorkflowEngine;
using Hris.Infrastructure;
using Hris.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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
//
// Destructure.With<SensitiveDataDestructuringPolicy>() -- monitoring-and-alerting.md's
// own Structured Logging section (NFR-OB-001): redaction of a [SensitiveData]-marked
// property must be structural, applied globally here, never left to per-call-site
// discipline. See that policy's own remarks for why no concrete type carries the
// attribute yet this Sprint.
builder.Host.UseSerilog((_, loggerConfiguration) =>
    loggerConfiguration
        .Destructure.With<SensitiveDataDestructuringPolicy>()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

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
// AddStatutoryReferenceDataFramework() is the eighth and last of Sprint 4's own eight
// frameworks. Unlike its three immediately preceding siblings above (Search, Scheduling,
// Job Processing), this framework deliberately carries no tenant context anywhere --
// statutory-reference-data.md's own Security Considerations state plainly that this
// data "is readable by every tenant; it is public information" and "is excluded from
// tenant data export, since it is not tenant data." Of its own three stated Upstream
// Dependencies (Localization, Configuration, Audit), none is concretely wired through
// MediatR this Sprint -- see DependencyInjection.cs for why each is a real dependency
// with no concrete integration point yet, not a gap. With this framework registered,
// all eight of Sprint 4's own frameworks are wired; Sprint 5 onward reads from this
// foundation rather than building it.
builder.Services.AddStatutoryReferenceDataFramework();
// AddWorkflowEngineFramework() is the first of Sprint 5's own two frameworks
// (Workflow Engine and Notification Framework), a genuine mutual dependency cycle per
// IMPLEMENTATION-PLAN.md's own Sprint 5 row, built as two separate PRs the same way
// every earlier Sprint's own multi-framework cycle (Sprint 3's nine-framework kernel,
// Sprint 4's own eight frameworks) already was. Of its own five stated Upstream
// Dependencies (Identity, Authorization, Rules Engine, Configuration, Notification),
// none is concretely wired through MediatR yet -- see DependencyInjection.cs for why
// each is a real dependency with no concrete integration point yet, not a gap.
builder.Services.AddWorkflowEngineFramework();
// AddNotificationFramework() is the second and last of Sprint 5's own two frameworks.
// Of its own five stated Upstream Dependencies (Workflow Engine, Rules Engine,
// Identity, Configuration, Event Framework), none is concretely wired through MediatR
// yet -- see DependencyInjection.cs for why each is a real dependency with no concrete
// integration point yet, not a gap. With this framework registered, both of Sprint 5's
// own two frameworks are wired; Sprint 6 (Entitlement & Process Pack Framework) is next.
builder.Services.AddNotificationFramework();
// AddEntitlementFramework() is Sprint 6's own single framework -- no MediatR
// requests of any other framework are wired to it and it takes no concrete
// ProjectReference of its own (entitlement-framework.md's own Dependencies section:
// "explicit reference, no concrete cross-framework dependency"), so its registration
// order relative to every framework above is not significant. With this framework
// registered, Sprint 6 is wired; Sprint 7 (API Platform) is next.
builder.Services.AddEntitlementFramework();
// AddCachingFramework() is the first of Sprint 8's own three frameworks
// (Kernel-Dependent Frameworks, Round 2), no forced order among them -- each lists
// exactly one Sprint 4 framework as its own additional dependency beyond the kernel
// (Caching needs Tenant Framework's own existence, per caching-framework.md's stated
// Upstream Dependencies, though no concrete MediatR call is wired to it this Sprint;
// see DependencyInjection.cs for why). No EF Core persistence of its own --
// caching-framework.md's own Scope section excludes "Permanent Data Storage" -- so
// registration order relative to AddHrisInfrastructure() below does not matter the
// way it does for a framework with its own IEntityTypeConfiguration<T>.
builder.Services.AddCachingFramework();
// AddDocumentManagementFramework() is the second of Sprint 8's own three frameworks.
// Unlike AddCachingFramework() above, this one owns real EF Core persistence
// (Document, DocumentAttachment), so it must run before AddHrisInfrastructure() for
// the same PersistenceAssemblyRegistry-ordering reason every persisted framework
// above it is registered in this order -- its own DependencyInjection.cs explains why
// none of its five stated Upstream Dependencies is concretely wired this Sprint.
builder.Services.AddDocumentManagementFramework();
// AddIntegrationFramework() is the third and last of Sprint 8's own three
// frameworks. Owns real EF Core persistence (Connector, IntegrationRun) so it must
// run before AddHrisInfrastructure() for the same PersistenceAssemblyRegistry-
// ordering reason every persisted framework above it is registered in this order --
// its own DependencyInjection.cs explains why none of its six stated Upstream
// Dependencies is concretely wired this Sprint. With this framework registered, all
// of Sprint 8 is wired; Sprint 9 (Logging & Monitoring Operational Layer) is next.
builder.Services.AddIntegrationFramework();
// AddOrganizationModule() is Phase 2 (Core HR) Sprint 1 -- the platform's first
// business module, following all nine Phase 1 (Foundation Platform) Sprints above.
// Owns real EF Core persistence (Organization, WorkLocation, LegalEntity) so it must
// run before AddHrisInfrastructure() for the same PersistenceAssemblyRegistry-
// ordering reason every persisted framework above it is registered in this order.
// No compile-time dependency on any Phase 1 framework or later Phase 2 module --
// this platform's own standing "reference by identifier, never by ProjectReference"
// rule, confirmed for this module in its own DependencyInjection.cs.
builder.Services.AddOrganizationModule();
// AddPositionModule() is Phase 2 (Core HR) Sprint 2 -- the platform's second
// business module, following Organization (Sprint 1) directly above. Owns real EF
// Core persistence (Position, JobFamily, JobClassification, JobGrade) so it must run
// before AddHrisInfrastructure() for the same PersistenceAssemblyRegistry-ordering
// reason every persisted framework/module above it is registered in this order. No
// compile-time dependency on Organization or any Phase 1 framework -- this
// platform's own standing "reference by identifier, never by ProjectReference" rule,
// confirmed for this module in its own DependencyInjection.cs.
builder.Services.AddPositionModule();
// AddEmploymentModule() is Phase 2 (Core HR) Sprint 3 -- the platform's third
// business module, following Organization (Sprint 1) and Position (Sprint 2) above.
// Owns real EF Core persistence (Employment, EmploymentContract,
// EmploymentAssignment) so it must run before AddHrisInfrastructure() for the same
// PersistenceAssemblyRegistry-ordering reason every persisted framework/module above
// it is registered in this order. No compile-time dependency on Organization,
// Position, or any Phase 1 framework, and none on the not-yet-built Employee module
// either -- this platform's own standing "reference by identifier, never by
// ProjectReference" rule, confirmed for this module in its own DependencyInjection.cs.
builder.Services.AddEmploymentModule();
// AddEmployeeModule() is Phase 2 (Core HR) Sprint 4 -- the platform's fourth and
// last Core HR business module, following Organization (Sprint 1), Position
// (Sprint 2), and Employment (Sprint 3) above -- "employee last of the four
// because it references employment" per IMPLEMENTATION-PLAN.md. Owns real EF Core
// persistence (Employee, EmployeeHistory) so it must run before
// AddHrisInfrastructure() for the same PersistenceAssemblyRegistry-ordering reason
// every persisted framework/module above it is registered in this order. No
// compile-time dependency on Organization, Position, Employment, or any Phase 1
// framework -- this platform's own standing "reference by identifier, never by
// ProjectReference" rule, confirmed for this module in its own
// DependencyInjection.cs.
builder.Services.AddEmployeeModule();
// AddAdministrationModule() is Phase 3 (Workforce Management) Sprint 1 -- the
// platform's first Phase 3 module, and first module overall since Phase 2 (Core
// HR) closed with Employee above. Owns real EF Core persistence (UserAccount,
// TenantRole, AdministrativeDelegation) so it must run before
// AddHrisInfrastructure() for the same PersistenceAssemblyRegistry-ordering
// reason every persisted framework/module above it is registered in this order.
// No compile-time dependency on any Phase 2 module or Phase 1 framework -- this
// platform's own standing "reference by identifier, never by ProjectReference"
// rule, confirmed for this module in its own DependencyInjection.cs.
builder.Services.AddAdministrationModule();
// AddWorkflowModule() is Phase 3 (Workforce Management) Sprint 2. It owns the
// business semantics of approval -- which processes require it, who approves, in
// what order -- and is deliberately NOT the Workflow Engine framework registered
// further above, which executes what this module defines. Owns real EF Core
// persistence (WorkflowDefinition and its owned steps, ApprovalPolicy,
// ApprovalDelegation) so it must run before AddHrisInfrastructure() for the same
// PersistenceAssemblyRegistry-ordering reason every persisted framework/module
// above it is registered in this order. No compile-time dependency on the Workflow
// Engine or on any sibling module, per this platform's own standing "reference by
// identifier, never by ProjectReference" rule.
builder.Services.AddWorkflowModule();
builder.Services.AddHrisInfrastructure(builder.Configuration);

// naming-conventions.md aside: this is a "readiness" check on the connection, not a
// query against any specific table, so it stays correct regardless of which
// framework's tables exist yet.
builder.Services.AddHealthChecks()
    .AddDbContextCheck<HrisDbContext>(name: "hris-database", tags: ["ready"]);

// Sprint 7 (API Platform, HEP-85) -- cross-cutting Presentation-layer infrastructure
// per api-standards.md, not a Foundation framework with its own aggregate. Registered
// here, after every AddXFramework() call, since none of it depends on any framework's
// own DI registration; GlobalExceptionHandler and OperationsEndpoints both dispatch
// through MediatR/ISender the same way every framework's own handler does, but they
// are Presentation-layer types living in this project, not a new Hris.Foundation.*
// project.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApiRateLimiting();

// Sprint 9 (Logging & Monitoring, Operational Layer, HEP-89) -- monitoring-and-alerting.md's
// own Distributed Tracing section (NFR-OB-002): "instruments every incoming request
// with a trace that follows the request through every layer it touches." Resource
// service name matches this host's own assembly, the identifier a trace backend uses
// to distinguish this platform's own traces from any other service emitting to the
// same collector.
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName: "Hris.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

var app = builder.Build();

// Runs first: every downstream middleware and endpoint executes inside this
// middleware's own LogContext.PushProperty scope, so the correlation id is already
// attached to the log entry UseSerilogRequestLogging() emits below, and to a Problem
// Details body GlobalExceptionHandler or an endpoint's own ToHttpResult() produces
// further downstream still.
app.UseCorrelationId();

// Sprint 9 (HEP-89) -- closes monitoring-and-alerting.md's own remaining Structured
// Logging gap (NFR-OB-001): TenantId/UserId join CorrelationId above in every log
// entry the rest of this pipeline produces, whenever a caller has populated them --
// see RequestContextEnrichmentMiddleware's own remarks for why no concrete caller
// does so yet this Sprint.
app.UseRequestContextEnrichment();

// api-standards.md's Error Response Format section, wired end to end: any exception
// (including a thrown FluentValidation.ValidationException -- "Invalid requests never
// reach the handler," application-pipeline.md) becomes the RFC 7807 Problem Details
// shape GlobalExceptionHandler builds, never a bare ASP.NET Core default error page.
app.UseExceptionHandler();

// logging-framework.md's own Log Categories section names "API Logs" (HTTP Method,
// Endpoint, Status Code, Duration) as one of the framework's five log categories;
// Serilog.AspNetCore's request-logging middleware is the concrete implementation of
// exactly that category, emitted through the same Serilog pipeline
// UseSerilog() configured above -- not a second, parallel logging mechanism.
app.UseSerilogRequestLogging();

// Enforces every RequireRateLimiting(...) policy name registered by
// AddApiRateLimiting() above -- without this middleware, an endpoint's own
// .RequireRateLimiting() call has no effect.
app.UseRateLimiter();

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

// api-standards.md's own Long-Running Operations section, first concrete endpoint:
// GET /api/v1/operations/{operationId}, a thin translation over Job Processing
// Framework's own already-existing GetJobQuery -- see OperationsEndpoints' own
// remarks for why this is not a new Foundation framework's own aggregate.
app.MapOperationsEndpoints();

app.Run();

/// <summary>
/// Microsoft.AspNetCore.Mvc.Testing's WebApplicationFactory&lt;TEntryPoint&gt; needs a
/// public type to reference -- top-level statements generate an internal Program
/// class by default. Hris.Api.Tests (Sprint 7's own new integration-test project, the
/// first to actually exercise this host's middleware pipeline rather than a
/// framework's own Domain/Application layer in isolation) references this partial
/// class directly.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1515:Consider making public types internal",
    Justification = "Must be public: WebApplicationFactory<Program> in Hris.Api.Tests " +
        "requires this type to be accessible from that separate test assembly. Not an " +
        "externally consumed API surface, the same reasoning PostgresContainerFixture's " +
        "own CA1515 suppression states for itself.")]
public partial class Program;
