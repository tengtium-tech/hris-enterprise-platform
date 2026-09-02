using Hris.Application.Abstractions;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Tenant.Application.Commands;

/// <summary>
/// Changes a tenant's own <see cref="Domain.Tenant.SubscriptionPlan"/> field while
/// Active, per tenant-framework.md's own <c>ChangeTenantSubscriptionPlanCommand</c>
/// row. That row also describes two further rules this Sprint's own handler cannot
/// enforce: automatically activating any pack the target edition newly includes by
/// default, and rejecting a downgrade that would remove a pack the tenant already
/// holds. Both require reading and changing `administration`'s own TenantConfiguration
/// Process Pack Activation state, which does not exist in code yet -- see
/// <see cref="Domain.Tenant.ChangeSubscriptionPlan"/>'s own remarks. This handler
/// performs only the field change Tenant itself owns; the pack-activation and
/// pack-removal-rejection rules land in a future pass that wires this command through
/// TenantConfiguration once it exists, not as an invented approximation here.
/// </summary>
public sealed record ChangeTenantSubscriptionPlanCommand(Guid TenantId, SubscriptionPlan NewSubscriptionPlan, Guid ChangedBy) : ICommand<Result>;

internal sealed class ChangeTenantSubscriptionPlanCommandHandler : IRequestHandler<ChangeTenantSubscriptionPlanCommand, Result>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ChangeTenantSubscriptionPlanCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ChangeTenantSubscriptionPlanCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.TenantNotFound);
        }

        return tenant.ChangeSubscriptionPlan(request.NewSubscriptionPlan, new PlatformOperatorId(request.ChangedBy), _timeProvider.GetUtcNow());
    }
}
