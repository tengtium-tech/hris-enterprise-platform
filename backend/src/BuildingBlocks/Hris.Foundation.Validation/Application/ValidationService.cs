using FluentValidation;
using Hris.Foundation.Configuration.Application.Queries;
using Hris.Foundation.Configuration.Domain;
using Hris.Foundation.Validation.Domain;
using Hris.SharedKernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Hris.Foundation.Validation.Application;

/// <summary>
/// The one implementation of <see cref="IValidationService"/>. Resolves every
/// registered <see cref="IValidator{T}"/> for the instance's own type, runs them,
/// and hands the combined result to <see cref="IValidationResultTranslator"/> along
/// with the <see cref="ValidationPolicy"/> resolved for the caller's own context --
/// the orchestration Logging Framework's own <c>LoggingService</c> describes for
/// itself: "Builds ... applies ... hands ... off," with the concrete translation
/// mechanics owned by Infrastructure.
///
/// Concretely exercises this framework's own "Upstream Dependencies: Configuration
/// Framework" line (validation-framework.md's Dependencies section) via
/// <see cref="ResolveConfigurationValueQuery"/> -- the same MediatR query every
/// other downstream consumer named in configuration-framework.md issues, and the
/// identical integration <c>LoggingService</c>'s own remarks establish for its
/// sibling minimum-severity lookup. This framework's other
/// upstream dependencies (Rules Engine, Logging, Audit, Localization) are
/// deliberately not wired here: nothing in this document names a concrete
/// integration point for any of them beyond what Configuration alone already
/// covers for policy resolution -- see this class's own remarks on Localization
/// specifically, and <c>DependencyInjection</c>'s own remarks on why Audit
/// Framework's write path is not called from here.
/// </summary>
internal sealed class ValidationService : IValidationService
{
    /// <summary>
    /// The well-known Configuration Framework key prefix this service resolves at
    /// Tenant (when supplied) then Global scope, per <see cref="ResolveConfigurationValueQuery"/>'s
    /// own <c>ScopeChain</c> priority-order contract. Suffixed with the caller's own
    /// <c>validationContext</c> rather than one flat key, per
    /// <see cref="Domain.ValidationPolicy"/>'s own remarks: "a tenant's chosen policy
    /// for a given validation context is actually stored" in Configuration Framework
    /// -- context-scoped, not one platform-wide setting, since validation-framework.md's
    /// own Validation Context section states "Validation behavior may vary by
    /// context."
    /// </summary>
    internal const string PolicyConfigurationKeyPrefix = "Validation.Policy.";

    /// <summary>
    /// validation-framework.md's own Validation Principles name "Validate Early" and
    /// "Fail Fast" first -- rejecting on any Error/Critical failure when no override
    /// is configured is the safe default those principles argue for, the inverse of
    /// <c>LoggingService</c>'s own choice of the most permissive level as its safe
    /// default: an unconfigured logging threshold should not silently stop logging,
    /// but an unconfigured validation policy should not silently let bad data
    /// through.
    /// </summary>
    private const ValidationPolicy _defaultPolicy = ValidationPolicy.RejectOnError;

    private readonly IServiceProvider _serviceProvider;
    private readonly IValidationResultTranslator _translator;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public ValidationService(
        IServiceProvider serviceProvider,
        IValidationResultTranslator translator,
        ISender sender,
        TimeProvider timeProvider)
    {
        _serviceProvider = Guard.AgainstNull(serviceProvider, nameof(serviceProvider));
        _translator = Guard.AgainstNull(translator, nameof(translator));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<ValidationOutcome> ValidateAsync<T>(
        T instance,
        string validationContext,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
        where T : class
    {
        Guard.AgainstNull(instance, nameof(instance));
        Guard.AgainstNullOrWhiteSpace(validationContext, nameof(validationContext));

        var policy = await ResolvePolicyAsync(validationContext, tenantId, cancellationToken).ConfigureAwait(false);

        // IServiceProvider, not a constructor-injected IEnumerable<IValidator<T>>:
        // this method is generic over T while the class itself is not, so the set of
        // validators to resolve is only known per call, the same reason
        // ValidationBehavior<TRequest, TResponse> can constructor-inject its own
        // IEnumerable<IValidator<TRequest>> (it is generic over the class itself)
        // and this service cannot. Registered Scoped (see DependencyInjection's own
        // remarks) specifically so this resolves from the caller's own request
        // scope, not the root container -- FluentValidation's own
        // AddValidatorsFromAssembly registers validators Scoped by default, and
        // resolving a Scoped service from a Singleton's captured root provider is
        // exactly the captive-dependency bug ASP.NET Core's own scope validation
        // rejects at runtime.
        var validators = _serviceProvider.GetServices<IValidator<T>>().ToList();
        if (validators.Count == 0)
        {
            return ValidationOutcome.Clean(policy);
        }

        var context = new ValidationContext<T>(instance);
        var results = await Task.WhenAll(
                validators.Select(validator => validator.ValidateAsync(context, cancellationToken)))
            .ConfigureAwait(false);

        var combined = new FluentValidation.Results.ValidationResult(results.SelectMany(result => result.Errors));
        return _translator.Translate(combined, policy);
    }

    private async Task<ValidationPolicy> ResolvePolicyAsync(
        string validationContext,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var scopeChain = new List<ConfigurationScope> { ConfigurationScope.Global() };

        if (tenantId is { } tenant)
        {
            var tenantScopeResult = ConfigurationScope.Create(ConfigurationScopeLevel.Tenant, tenant);
            if (tenantScopeResult.IsFailure)
            {
                // A caller passing Guid.Empty as its own tenant id is a contract
                // violation (the same class of caller mistake LoggingService.LogAsync's
                // own remarks describe for an empty correlation id), not a runtime
                // business outcome -- fails fast rather than silently validating
                // against the wrong scope.
                throw new ArgumentException(tenantScopeResult.Error.Description, nameof(tenantId));
            }

            // Tenant first, Global last: ResolveConfigurationValueQuery's own
            // ScopeChain is priority-ordered, most specific first, per
            // configuration-framework.md's Configuration Hierarchy ("More specific
            // configuration overrides higher-level defaults").
            scopeChain.Insert(0, tenantScopeResult.Value);
        }

        var query = new ResolveConfigurationValueQuery(
            PolicyConfigurationKeyPrefix + validationContext,
            scopeChain,
            DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime));

        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        // No configured override resolves to the safe default rather than
        // propagating ConfigurationErrors.VersionNotFound -- validation-framework.md's
        // own Availability requirement ("Validation services should remain
        // continuously available") means the absence of an operator-set policy must
        // never stop validation from running, the identical reasoning
        // LoggingService.ResolveMinimumSeverityAsync's own remarks give for its own
        // missing-override case.
        return result.IsSuccess && Enum.TryParse<ValidationPolicy>(result.Value, ignoreCase: true, out var parsed)
            ? parsed
            : _defaultPolicy;
    }
}
