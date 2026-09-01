using Hris.Foundation.Validation.Application;
using Hris.Foundation.Validation.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Validation;

/// <summary>
/// Validation Framework's single registration entry point, per module-registration.md's
/// Module Entry Point section -- the same one-extension-method convention every
/// other Sprint 3 framework's own registration follows.
///
/// Like Logging Framework's own registration, this does not call
/// <c>PersistenceAssemblyRegistry.Register</c> (no <c>IEntityTypeConfiguration</c> --
/// this framework persists nothing through <c>HrisDbContext</c>) or <c>AddMediatR</c>
/// for this framework's own assembly (no MediatR requests of its own -- see
/// <see cref="IValidationService"/>'s own remarks). It also does not call
/// <c>AddValidatorsFromAssembly</c> for itself: this framework defines no
/// <c>AbstractValidator&lt;T&gt;</c> of its own, only reusable rule-builder
/// extension methods (<see cref="GovernmentIdentifierRuleBuilderExtensions"/>) other
/// frameworks' own already-registered validators call directly -- there is nothing
/// here for that scan to find. <see cref="ValidationService"/>'s own
/// <c>IServiceProvider.GetServices&lt;IValidator&lt;T&gt;&gt;()</c> call resolves
/// whatever validators each calling framework's own <c>AddValidatorsFromAssembly</c>
/// registered for that framework's own assembly, at the point a caller actually
/// invokes <see cref="IValidationService"/>'s own <c>ValidateAsync</c>.
///
/// This framework's own Audit Framework upstream dependency is deliberately not
/// wired here either: routing every validation call through
/// <c>IAuditRecorder</c> would mean a database write on this framework's own
/// stated "millions of validation requests per day" hot path (Scalability NFR) --
/// the identical reasoning Rules Engine's own <c>EvaluateRuleQuery</c> and
/// Authorization Framework's own <c>CheckAuthorizationQuery</c> already establish
/// for not publishing an event on every one of their own high-volume calls. Nothing
/// in validation-framework.md names a concrete "audit every validation" requirement
/// that would outweigh that cost; "Validation Audit Logging" in its own Security
/// Considerations is left for a caller that already writes its own audit trail (a
/// business module recording a rejected submission, say) to include the validation
/// outcome in that write, not for this framework to duplicate.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddValidationFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Singleton: FluentValidationOutcomeTranslator holds no per-request state,
        // the same reasoning SerilogLogSink's own registration gives for its own
        // singleton lifetime.
        services.AddSingleton<IValidationResultTranslator, FluentValidationOutcomeTranslator>();

        // Scoped, not Singleton like ILoggingService's own registration: unlike
        // LoggingService, ValidationService resolves IValidator<T> instances from
        // its own injected IServiceProvider at call time, and FluentValidation's own
        // AddValidatorsFromAssembly registers those Scoped by default -- see
        // ValidationService's own remarks on why a Singleton holding a captured root
        // provider would be a captive-dependency bug here.
        services.AddScoped<IValidationService, ValidationService>();

        return services;
    }
}
