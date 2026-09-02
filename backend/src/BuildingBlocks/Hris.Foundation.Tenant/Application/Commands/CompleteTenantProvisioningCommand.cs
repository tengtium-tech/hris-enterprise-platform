using Hris.Application.Abstractions;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Tenant.Application.Commands;

/// <summary>
/// The Application-layer trigger for <see cref="Domain.Tenant.CompleteProvisioning"/>
/// -- Provisioning -&gt; Configured, per the State Machine's own "Automated
/// provisioning completes" row. Not one of tenant-framework.md's own eight named
/// Platform-Operator-Facing Commands: that document describes this transition as
/// orchestration across several systems (Database Initialization, Configuration
/// Creation, Branding Initialization, Process Pack Activation, administrator
/// invitation dispatch), not a single actor's request, and none of those systems
/// exist in code yet to actually perform that orchestration from.
///
/// This command exists because <c>TenantProvisioned</c>'s own field list
/// (<c>TenantId, TenantConfigurationId</c>) is already fully specified in Domain
/// Events, and <see cref="Domain.Tenant"/>'s own Invariants require a real
/// TenantConfiguration to exist before Active is reachable -- implementing the
/// mechanical trigger for an event the document has already decided the shape of is
/// not inventing a decision (tenant-framework.md's own AI Implementation Guidance:
/// "Do not treat any of the three as still open... implementing them against the
/// commands named here is not inventing a decision, it is following one already
/// made" -- the same reasoning applied one level down, to the one transition that
/// document names with no command of its own). Once `administration`'s own
/// TenantConfiguration creation exists, the orchestration that calls this command
/// after Configuration Creation, Branding Initialization, Process Pack Activation,
/// and invitation dispatch all genuinely complete is that future integration's own
/// responsibility -- wiring a sibling in incrementally as it comes online, this
/// session's own established pattern.
/// </summary>
public sealed record CompleteTenantProvisioningCommand(Guid TenantId, Guid TenantConfigurationId) : ICommand<Result>;

internal sealed class CompleteTenantProvisioningCommandHandler : IRequestHandler<CompleteTenantProvisioningCommand, Result>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CompleteTenantProvisioningCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(CompleteTenantProvisioningCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.TenantNotFound);
        }

        return tenant.CompleteProvisioning(new TenantConfigurationId(request.TenantConfigurationId), _timeProvider.GetUtcNow());
    }
}
