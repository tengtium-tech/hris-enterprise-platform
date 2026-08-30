using Hris.Application.Abstractions;
using Hris.Foundation.Configuration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Configuration.Application.Queries;

/// <summary>
/// The runtime "what value applies right now" query every downstream consumer named in
/// configuration-framework.md's Downstream Consumers section (Workflow Engine, Rules
/// Engine, Payroll, and the rest) issues -- thin wrapper over
/// <see cref="ConfigurationHierarchyResolver"/>, per configuration-framework.md's own
/// Configuration Hierarchy: "More specific configuration overrides higher-level
/// defaults."
///
/// <paramref name="ScopeChain"/> is supplied entirely by the caller, never inferred
/// here from ambient tenant/session state, per this framework's own
/// <see cref="ConfigurationHierarchyResolver"/> remarks and `CTR-ISO-003` ("Isolation
/// Enforced Below the Application Layer") -- a caller that omits its own Tenant scope
/// from the chain gets Global's value, not a silently-assumed tenant.
/// </summary>
public sealed record ResolveConfigurationValueQuery(
    string Key,
    IReadOnlyCollection<ConfigurationScope> ScopeChain,
    DateOnly AsOfDate) : IQuery<Result<string>>;

internal sealed class ResolveConfigurationValueQueryHandler
    : IRequestHandler<ResolveConfigurationValueQuery, Result<string>>
{
    private readonly ConfigurationHierarchyResolver _resolver;

    public ResolveConfigurationValueQueryHandler(ConfigurationHierarchyResolver resolver)
    {
        _resolver = Guard.AgainstNull(resolver, nameof(resolver));
    }

    public async Task<Result<string>> Handle(ResolveConfigurationValueQuery request, CancellationToken cancellationToken)
    {
        var keyResult = ConfigurationKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<string>(keyResult.Error);
        }

        return await _resolver
            .ResolveAsync(keyResult.Value, request.ScopeChain, request.AsOfDate, cancellationToken)
            .ConfigureAwait(false);
    }
}
