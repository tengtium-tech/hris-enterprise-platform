using Hris.Application.Abstractions;
using Hris.Modules.Workflow.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Workflow.Application.Commands;

/// <summary>
/// Establishes the tenant's single approval policy (WR-030). Separate from
/// <see cref="ConfigureApprovalPolicyCommand"/> because creating the tenant's
/// governance record and changing it are different acts with different audit
/// weight, and only the second needs a reason.
/// </summary>
public sealed record CreateApprovalPolicyCommand(Guid TenantId, Guid ActingUser) : ICommand<Result<Guid>>;

internal sealed class CreateApprovalPolicyCommandHandler : IRequestHandler<CreateApprovalPolicyCommand, Result<Guid>>
{
    private readonly IApprovalPolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateApprovalPolicyCommandHandler(IApprovalPolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateApprovalPolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var existing = await _repository.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        var result = ApprovalPolicy.Create(
            new ApprovalPolicyId(Guid.NewGuid()), request.TenantId, existing is not null, _timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        await _repository.AddAsync(result.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(result.Value.Id.Value);
    }
}

/// <summary>
/// One command covers the whole policy surface, per commands.md: each value changes
/// what is permitted tenant-wide, each requires the same permission, and each
/// belongs in access-review reporting rather than a routine configuration log.
///
/// There is deliberately no field here for a self-approval setting.
/// <see cref="ApprovalPolicy"/> holds none to configure (WR-032), and that is not an
/// omission to be filled in later. A reason is required, because a policy change
/// that stops requiring approval for a business process is a control removal.
///
/// <see cref="AnyLimitExceedsRoleStanding"/> is WR-031, carried as a caller-supplied
/// signal because deciding it needs Authorization Framework data this module does
/// not own.
/// </summary>
public sealed record ConfigureApprovalPolicyCommand(
    Guid TenantId,
    IReadOnlyList<Guid>? ProcessesRequiringApproval,
    TimeSpan? DefaultEscalationTriggerAfter,
    ApproverResolutionKind? DefaultEscalationTargetKind,
    string? DefaultEscalationTargetRoleName,
    Guid? DefaultEscalationTargetScopeId,
    int? DefaultEscalationMaxDepth,
    TimeSpan? DefaultSla,
    IReadOnlyList<ApprovalAuthorityLimit>? AuthorityLimits,
    bool CustomDefinitionsPermitted,
    bool AnyLimitExceedsRoleStanding,
    Guid ActingUser,
    string? Reason) : ICommand<Result>;

internal sealed class ConfigureApprovalPolicyCommandHandler : IRequestHandler<ConfigureApprovalPolicyCommand, Result>
{
    private readonly IApprovalPolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfigureApprovalPolicyCommandHandler(IApprovalPolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ConfigureApprovalPolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await WorkflowLookup
            .LoadPolicyForTenantAsync(_repository, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        EscalationPolicy? escalation = null;
        if (request.DefaultEscalationTriggerAfter is not null)
        {
            if (request.DefaultEscalationTargetKind is null)
            {
                return Result.Failure(WorkflowErrors.RoleResolutionRequiresRoleName);
            }

            var targetResult = request.DefaultEscalationTargetKind.Value switch
            {
                ApproverResolutionKind.Role => ApproverResolutionRule.ForRole(request.DefaultEscalationTargetRoleName),
                ApproverResolutionKind.OrganizationalScope => ApproverResolutionRule.ForOrganizationalScope(
                    request.DefaultEscalationTargetRoleName, request.DefaultEscalationTargetScopeId),
                _ => ApproverResolutionRule.ForReportingLine(),
            };

            if (targetResult.IsFailure)
            {
                return Result.Failure(targetResult.Error);
            }

            var escalationResult = EscalationPolicy.Create(
                request.DefaultEscalationTriggerAfter.Value, targetResult.Value, request.DefaultEscalationMaxDepth ?? 0);
            if (escalationResult.IsFailure)
            {
                return Result.Failure(escalationResult.Error);
            }

            escalation = escalationResult.Value;
        }

        SlaDuration? sla = null;
        if (request.DefaultSla is not null)
        {
            var slaResult = SlaDuration.Create(request.DefaultSla.Value);
            if (slaResult.IsFailure)
            {
                return Result.Failure(slaResult.Error);
            }

            sla = slaResult.Value;
        }

        return policyResult.Value.Configure(
            request.ProcessesRequiringApproval, escalation, sla, request.AuthorityLimits, request.CustomDefinitionsPermitted,
            request.AnyLimitExceedsRoleStanding, request.ActingUser, request.Reason, _timeProvider.GetUtcNow());
    }
}
