using Hris.Application.Abstractions;
using Hris.Modules.Leave.Application;
using Hris.Modules.Leave.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Leave.Application.Commands;

/// <summary>Authors a new policy as Draft. Source: application/commands.md (LeavePolicy Commands).</summary>
public sealed record DefineLeavePolicyCommand(
    Guid TenantId, Guid LeaveTypeId, LeavePolicyRuleset Ruleset, Guid CreatedBy) : ICommand<Result<Guid>>;

internal sealed class DefineLeavePolicyCommandHandler : IRequestHandler<DefineLeavePolicyCommand, Result<Guid>>
{
    private readonly ILeavePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DefineLeavePolicyCommandHandler(ILeavePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(DefineLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var createResult = LeavePolicy.Create(
            new LeavePolicyId(Guid.NewGuid()), request.TenantId, new LeaveTypeId(request.LeaveTypeId), request.Ruleset,
            request.CreatedBy, _timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>
/// Publishes a Draft policy to Active with an effective date. Rejected if a statutory
/// leave type's policy configures below its statutory minimum (LV-013) — the one check
/// that must read both this policy and its <see cref="LeaveType"/>, so it lives here
/// rather than inside either aggregate (Aggregate Design Rule 13).
/// </summary>
public sealed record PublishLeavePolicyCommand(
    Guid TenantId, Guid LeavePolicyId, DateOnly EffectiveFrom, Guid ActorId) : ICommand<Result>;

internal sealed class PublishLeavePolicyCommandHandler : IRequestHandler<PublishLeavePolicyCommand, Result>
{
    private readonly ILeavePolicyRepository _repository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly TimeProvider _timeProvider;

    public PublishLeavePolicyCommandHandler(
        ILeavePolicyRepository repository, ILeaveTypeRepository leaveTypeRepository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _leaveTypeRepository = Guard.AgainstNull(leaveTypeRepository, nameof(leaveTypeRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await LeaveLookup
            .LoadLeavePolicyForTenantAsync(_repository, request.LeavePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        var leaveType = await _leaveTypeRepository
            .GetByIdAsync(policyResult.Value.LeaveTypeId, cancellationToken)
            .ConfigureAwait(false);

        if (leaveType is { Category: LeaveTypeCategory.Statutory, StatutoryMinimum: { } statutoryMinimum }
            && policyResult.Value.Ruleset.EntitlementCap.MaximumAccruable < statutoryMinimum)
        {
            return Result.Failure(LeaveErrors.PolicyBelowStatutoryMinimum);
        }

        return policyResult.Value.Publish(request.EffectiveFrom, request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>Produces the next version of a policy as a new Draft (LV-010); never edits the current one.</summary>
public sealed record ReviseLeavePolicyCommand(
    Guid TenantId, Guid LeavePolicyId, LeavePolicyRuleset Ruleset, DateOnly NewEffectiveFrom, Guid ChangedBy)
    : ICommand<Result<Guid>>;

internal sealed class ReviseLeavePolicyCommandHandler : IRequestHandler<ReviseLeavePolicyCommand, Result<Guid>>
{
    private readonly ILeavePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReviseLeavePolicyCommandHandler(ILeavePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(ReviseLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await LeaveLookup
            .LoadLeavePolicyForTenantAsync(_repository, request.LeavePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure<Guid>(policyResult.Error);
        }

        var reviseResult = policyResult.Value.Revise(
            new LeavePolicyId(Guid.NewGuid()), request.Ruleset, request.NewEffectiveFrom, request.ChangedBy,
            _timeProvider.GetUtcNow());

        if (reviseResult.IsFailure)
        {
            return Result.Failure<Guid>(reviseResult.Error);
        }

        await _repository.AddAsync(reviseResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(reviseResult.Value.Id.Value);
    }
}

/// <summary>Binds a policy to an organizational scope for an effective period.</summary>
public sealed record AssignLeavePolicyCommand(
    Guid TenantId,
    Guid LeavePolicyId,
    Guid PolicyAssignmentId,
    LeavePolicyScopeLevel ScopeLevel,
    string ScopeTargetId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid AssignedBy) : ICommand<Result>;

internal sealed class AssignLeavePolicyCommandHandler : IRequestHandler<AssignLeavePolicyCommand, Result>
{
    private readonly ILeavePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AssignLeavePolicyCommandHandler(ILeavePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(AssignLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await LeaveLookup
            .LoadLeavePolicyForTenantAsync(_repository, request.LeavePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        return policyResult.Value.Assign(
            new PolicyAssignmentId(request.PolicyAssignmentId), request.ScopeLevel, request.ScopeTargetId,
            request.EffectiveFrom, request.EffectiveTo, request.AssignedBy, _timeProvider.GetUtcNow());
    }
}

/// <summary>End-dates a policy assignment rather than deleting it.</summary>
public sealed record UnassignLeavePolicyCommand(
    Guid TenantId, Guid LeavePolicyId, Guid PolicyAssignmentId, DateOnly EffectiveTo, Guid ActorId) : ICommand<Result>;

internal sealed class UnassignLeavePolicyCommandHandler : IRequestHandler<UnassignLeavePolicyCommand, Result>
{
    private readonly ILeavePolicyRepository _repository;

    public UnassignLeavePolicyCommandHandler(ILeavePolicyRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result> Handle(UnassignLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await LeaveLookup
            .LoadLeavePolicyForTenantAsync(_repository, request.LeavePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        return policyResult.Value.Unassign(new PolicyAssignmentId(request.PolicyAssignmentId), request.EffectiveTo);
    }
}

/// <summary>Retires a policy version.</summary>
public sealed record RetireLeavePolicyCommand(Guid TenantId, Guid LeavePolicyId, Guid ActorId, string Reason) : ICommand<Result>;

internal sealed class RetireLeavePolicyCommandHandler : IRequestHandler<RetireLeavePolicyCommand, Result>
{
    private readonly ILeavePolicyRepository _repository;

    public RetireLeavePolicyCommandHandler(ILeavePolicyRepository repository) =>
        _repository = Guard.AgainstNull(repository, nameof(repository));

    public async Task<Result> Handle(RetireLeavePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await LeaveLookup
            .LoadLeavePolicyForTenantAsync(_repository, request.LeavePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        return policyResult.Value.Retire();
    }
}
