using Hris.Application.Abstractions;
using Hris.Modules.Attendance.Application;
using Hris.Modules.Attendance.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Attendance.Application.Commands;

/// <summary>Authors a new policy as Draft. Source: application/commands.md (AttendancePolicy Commands).</summary>
public sealed record DefineAttendancePolicyCommand(
    Guid TenantId,
    string Name,
    PolicyCalculationConfiguration Configuration,
    DateOnly EffectiveFrom,
    Guid CreatedBy) : ICommand<Result<Guid>>;

internal sealed class DefineAttendancePolicyCommandHandler
    : IRequestHandler<DefineAttendancePolicyCommand, Result<Guid>>
{
    private readonly IAttendancePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DefineAttendancePolicyCommandHandler(IAttendancePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(DefineAttendancePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var createResult = AttendancePolicy.Create(
            new AttendancePolicyId(Guid.NewGuid()),
            request.TenantId,
            request.Name,
            request.Configuration,
            request.EffectiveFrom,
            request.CreatedBy,
            _timeProvider.GetUtcNow());

        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);
        return Result.Success(createResult.Value.Id.Value);
    }
}

/// <summary>Publishes a Draft policy to Active with an effective date.</summary>
public sealed record PublishAttendancePolicyCommand(
    Guid TenantId, Guid AttendancePolicyId, DateOnly EffectiveFrom, Guid ActorId) : ICommand<Result>;

internal sealed class PublishAttendancePolicyCommandHandler : IRequestHandler<PublishAttendancePolicyCommand, Result>
{
    private readonly IAttendancePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishAttendancePolicyCommandHandler(IAttendancePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishAttendancePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await AttendanceLookup
            .LoadPolicyForTenantAsync(_repository, request.AttendancePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        return policyResult.Value.Publish(request.EffectiveFrom, request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>Produces the next version of a policy as a new Draft (AT-002); never edits the current one.</summary>
public sealed record ReviseAttendancePolicyCommand(
    Guid TenantId,
    Guid AttendancePolicyId,
    PolicyCalculationConfiguration Configuration,
    DateOnly NewEffectiveFrom,
    Guid ChangedBy) : ICommand<Result<Guid>>;

internal sealed class ReviseAttendancePolicyCommandHandler : IRequestHandler<ReviseAttendancePolicyCommand, Result<Guid>>
{
    private readonly IAttendancePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReviseAttendancePolicyCommandHandler(IAttendancePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(ReviseAttendancePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await AttendanceLookup
            .LoadPolicyForTenantAsync(_repository, request.AttendancePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure<Guid>(policyResult.Error);
        }

        var reviseResult = policyResult.Value.Revise(
            new AttendancePolicyId(Guid.NewGuid()),
            request.Configuration,
            request.NewEffectiveFrom,
            request.ChangedBy,
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
public sealed record AssignAttendancePolicyCommand(
    Guid TenantId,
    Guid AttendancePolicyId,
    Guid PolicyAssignmentId,
    PolicyScopeLevel ScopeLevel,
    string ScopeTargetId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    Guid AssignedBy) : ICommand<Result>;

internal sealed class AssignAttendancePolicyCommandHandler : IRequestHandler<AssignAttendancePolicyCommand, Result>
{
    private readonly IAttendancePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AssignAttendancePolicyCommandHandler(IAttendancePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(AssignAttendancePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await AttendanceLookup
            .LoadPolicyForTenantAsync(_repository, request.AttendancePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        return policyResult.Value.Assign(
            new PolicyAssignmentId(request.PolicyAssignmentId),
            request.ScopeLevel,
            request.ScopeTargetId,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.AssignedBy,
            _timeProvider.GetUtcNow());
    }
}

/// <summary>End-dates a policy assignment rather than deleting it.</summary>
public sealed record UnassignAttendancePolicyCommand(
    Guid TenantId, Guid AttendancePolicyId, Guid PolicyAssignmentId, DateOnly EffectiveTo, Guid ActorId) : ICommand<Result>;

internal sealed class UnassignAttendancePolicyCommandHandler : IRequestHandler<UnassignAttendancePolicyCommand, Result>
{
    private readonly IAttendancePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UnassignAttendancePolicyCommandHandler(IAttendancePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UnassignAttendancePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await AttendanceLookup
            .LoadPolicyForTenantAsync(_repository, request.AttendancePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        return policyResult.Value.Unassign(
            new PolicyAssignmentId(request.PolicyAssignmentId), request.EffectiveTo, request.ActorId, _timeProvider.GetUtcNow());
    }
}

/// <summary>Retires a policy version.</summary>
public sealed record RetireAttendancePolicyCommand(Guid TenantId, Guid AttendancePolicyId, Guid ActorId, string Reason)
    : ICommand<Result>;

internal sealed class RetireAttendancePolicyCommandHandler : IRequestHandler<RetireAttendancePolicyCommand, Result>
{
    private readonly IAttendancePolicyRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RetireAttendancePolicyCommandHandler(IAttendancePolicyRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetireAttendancePolicyCommand request, CancellationToken cancellationToken)
    {
        Guard.AgainstNull(request, nameof(request));

        var policyResult = await AttendanceLookup
            .LoadPolicyForTenantAsync(_repository, request.AttendancePolicyId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);
        if (policyResult.IsFailure)
        {
            return Result.Failure(policyResult.Error);
        }

        return policyResult.Value.Retire(request.ActorId, request.Reason, _timeProvider.GetUtcNow());
    }
}
