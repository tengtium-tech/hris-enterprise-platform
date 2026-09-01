using Hris.Application.Abstractions;
using Hris.Foundation.Configuration.Application.Queries;
using Hris.Foundation.Configuration.Domain;
using Hris.Foundation.Localization.Application.Commands;
using Hris.Foundation.Localization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Localization.Application.Queries;

/// <summary>
/// Resolves one translation key's text for a locale, per localization-framework.md's
/// own Downstream Consumers list -- the query User Interface, Reporting, and every
/// other named consumer issues to render localized text. Deliberately ungated and
/// deliberately does not publish <see cref="Domain.TranslationUpdated"/> or any other
/// carried event: the identical Scalability-NFR reasoning already established for
/// Rules Engine's own <c>EvaluateRuleQuery</c> and Authorization Framework's own
/// <c>CheckAuthorizationQuery</c> -- a page render or report generation calling this
/// is exactly the high-volume, no-persistence-needed path those two queries' own
/// remarks describe, and this document's own Scalability NFR ("hundreds of locales
/// and thousands of translation resources across multiple tenants") names the same
/// concern for this framework.
///
/// Concretely exercises this framework's own "Upstream Dependencies: Configuration
/// Framework" line: <see cref="TranslationEntry.Resolve"/> needs a fallback chain
/// the *caller* supplies, per that method's own remarks ("the caller supplies the
/// configured chain... rather than this type assuming one") -- this handler is that
/// caller, resolving the configured chain through <see cref="ResolveConfigurationValueQueryHandler"/>
/// rather than requiring every consumer of this query to know and pass its own copy,
/// the same "Fallback behavior should be configurable" requirement this document's
/// own Translation Management section states. <paramref name="TenantId"/> lets the
/// *chain itself* vary per tenant (a tenant with mostly Filipino-fluent staff might
/// configure "fil-PH" ahead of "en-US" in its own fallback order) even though the
/// underlying <see cref="TranslationEntry"/> catalog this query reads from is
/// platform-wide, not tenant-scoped -- see <see cref="CreateTranslationEntryCommand"/>'s
/// own remarks for why the catalog itself has no tenant field to key against.
/// </summary>
public sealed record ResolveTranslationQuery(
    string Key,
    string Locale,
    Guid? TenantId = null) : IQuery<Result<string>>;

internal sealed class ResolveTranslationQueryHandler : IRequestHandler<ResolveTranslationQuery, Result<string>>
{
    /// <summary>
    /// The well-known Configuration Framework key this handler resolves at Tenant
    /// (when supplied) then Global scope. Value is a comma-separated ordered list of
    /// locale tags (e.g. <c>"en-US,en-GB"</c>) -- a plain delimited string, not JSON,
    /// matching <c>ConfigurationSetting.Value</c>'s own single-string shape rather
    /// than inventing a structured Configuration value type this framework alone
    /// would need.
    /// </summary>
    internal const string FallbackLocalesConfigurationKey = "Localization.FallbackLocales";

    private readonly ITranslationEntryRepository _repository;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public ResolveTranslationQueryHandler(
        ITranslationEntryRepository repository,
        ISender sender,
        TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<string>> Handle(ResolveTranslationQuery request, CancellationToken cancellationToken)
    {
        var localeResult = Locale.Create(request.Locale);
        if (localeResult.IsFailure)
        {
            return Result.Failure<string>(localeResult.Error);
        }

        var entry = await _repository.GetByKeyAsync(request.Key, cancellationToken).ConfigureAwait(false);
        if (entry is null)
        {
            return Result.Failure<string>(LocalizationErrors.TranslationEntryNotFound);
        }

        var fallbackChain = await ResolveFallbackChainAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return entry.Resolve(localeResult.Value, fallbackChain);
    }

    private async Task<IReadOnlyList<Locale>> ResolveFallbackChainAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        var scopeChain = new List<ConfigurationScope> { ConfigurationScope.Global() };

        if (tenantId is { } tenant)
        {
            var tenantScopeResult = ConfigurationScope.Create(ConfigurationScopeLevel.Tenant, tenant);
            if (tenantScopeResult.IsFailure)
            {
                // A caller passing Guid.Empty as its own tenant id is a contract
                // violation, not a runtime business outcome -- the identical
                // fail-fast reasoning ValidationService.ResolvePolicyAsync's own
                // remarks give for the same case.
                throw new ArgumentException(tenantScopeResult.Error.Description, nameof(tenantId));
            }

            scopeChain.Insert(0, tenantScopeResult.Value);
        }

        var query = new ResolveConfigurationValueQuery(
            FallbackLocalesConfigurationKey,
            scopeChain,
            DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime));

        var result = await _sender.Send(query, cancellationToken).ConfigureAwait(false);

        // No configured fallback chain resolves to an empty chain, not a thrown
        // error -- localization-framework.md's own Availability requirement
        // ("Localization services should remain continuously available") means an
        // unconfigured fallback list must not stop a caller that only wanted the
        // exact-locale text (Resolve already succeeds without ever reaching the
        // fallback chain when the exact locale exists), the identical reasoning
        // LoggingService.ResolveMinimumSeverityAsync's own remarks give for its own
        // missing-override case.
        if (result.IsFailure)
        {
            return [];
        }

        var fallbackLocales = new List<Locale>();
        foreach (var tag in result.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var tagResult = Locale.Create(tag);
            if (tagResult.IsSuccess)
            {
                fallbackLocales.Add(tagResult.Value);
            }

            // A malformed entry in an operator-configured fallback list is skipped,
            // not fatal to the whole resolution -- the same "do not let bad
            // configuration break an otherwise-available request" principle this
            // method's own remarks already state for a missing configuration value
            // entirely.
        }

        return fallbackLocales;
    }
}
