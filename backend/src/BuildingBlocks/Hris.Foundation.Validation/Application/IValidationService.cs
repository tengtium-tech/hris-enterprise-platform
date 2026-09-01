using Hris.Foundation.Validation.Domain;

namespace Hris.Foundation.Validation.Application;

/// <summary>
/// The Application-layer facade other code -- other frameworks' own Application
/// layers, and eventually business modules -- calls to run structural validation
/// and get back this framework's own richer, policy-aware
/// <see cref="ValidationOutcome"/>, per validation-framework.md's own framing:
/// "Business modules should use the Validation Framework instead of implementing
/// independent validation logic."
///
/// Deliberately not a MediatR <c>ICommand</c>/<c>IQuery</c>, the same shape choice
/// <c>ILoggingService</c>'s own remarks explain for Logging Framework: nothing in
/// validation-framework.md describes a user-driven business command lifecycle for
/// "run this validator" -- it happens inline, wherever a caller already holds the
/// instance to validate, not submitted through an API endpoint.
///
/// Distinct from -- and does not replace -- <c>Hris.Application.Behaviors.ValidationBehavior</c>,
/// the pipeline behavior every MediatR request already runs through
/// (application-pipeline.md's own Validation Behavior: "Required fields, Data
/// formats, Input consistency, Business-independent validation... Invalid requests
/// never reach the handler"). That behavior rejects on the first
/// <c>FluentValidation.ValidationException</c> with no configurable policy, no
/// severity distinction, and no context-awareness -- correct for its own narrower
/// job of gatekeeping a MediatR request before the handler runs. This service is
/// the separate, richer capability validation-framework.md's own Scope names
/// beyond that: multi-failure reporting or bulk/import validation with configurable
/// policy, tenant-aware context resolution.
/// </summary>
public interface IValidationService
{
    /// <param name="instance">
    /// The object to validate against every registered
    /// <see cref="FluentValidation.IValidator{T}"/> for <typeparamref name="T"/>.
    /// Constrained to reference types only -- every command, query, and DTO this
    /// call is meant to validate already is one, and <c>Guard.AgainstNull</c>'s own
    /// signature requires it.
    /// </param>
    /// <param name="validationContext">
    /// Which of validation-framework.md's own Validation Context examples this call
    /// represents (e.g. "ApiValidation", "BatchImport") -- caller-supplied, never
    /// inferred, per this document's own "Validation behavior may vary by context."
    /// Also the key suffix <see cref="ValidationService"/> resolves the
    /// currently-configured <see cref="ValidationPolicy"/> under (see that class's
    /// own remarks).
    /// </param>
    /// <param name="tenantId">
    /// The current tenant, if any -- resolved into the Configuration Framework scope
    /// chain <see cref="ValidationService"/> queries, per this document's own
    /// Security Considerations ("Tenant-Aware Validation"). Left to the caller
    /// rather than resolved from ambient state here, the same `CTR-ISO-003`
    /// reasoning <c>ResolveConfigurationValueQuery</c>'s own remarks state for its
    /// own <c>ScopeChain</c>.
    /// </param>
    Task<ValidationOutcome> ValidateAsync<T>(
        T instance,
        string validationContext,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
        where T : class;
}
