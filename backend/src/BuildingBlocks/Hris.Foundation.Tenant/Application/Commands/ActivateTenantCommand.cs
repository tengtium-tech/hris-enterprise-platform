using Hris.Application.Abstractions;
using Hris.Foundation.Identity.Domain;
using Hris.Foundation.Tenant.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Tenant.Application.Commands;

/// <summary>
/// Configured -&gt; Active, per tenant-framework.md's own <c>ActivateTenantCommand</c>
/// row. The one exception among the eight Platform-Operator-Facing commands: invoked
/// by the invited Tenant Administrator's own account, never a Platform Operator --
/// "the Platform Operator's own involvement ends here" -- so <see cref="ActivatedBy"/>
/// is a <see cref="UserAccountId"/>, not a <see cref="PlatformOperatorId"/>.
/// </summary>
public sealed record ActivateTenantCommand(Guid TenantId, Guid ActivatedBy) : ICommand<Result>;

internal sealed class ActivateTenantCommandHandler : IRequestHandler<ActivateTenantCommand, Result>
{
    private readonly ITenantRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateTenantCommandHandler(ITenantRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateTenantCommand request, CancellationToken cancellationToken)
    {
        var tenant = await _repository.GetByIdAsync(new TenantId(request.TenantId), cancellationToken).ConfigureAwait(false);
        if (tenant is null)
        {
            return Result.Failure(TenantErrors.TenantNotFound);
        }

        return tenant.Activate(new UserAccountId(request.ActivatedBy), _timeProvider.GetUtcNow());
    }
}
