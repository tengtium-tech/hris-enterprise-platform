using Hris.Application.Abstractions;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Commands;

public sealed record SuspendEmploymentCommand(Guid EmploymentId, Guid TenantId, string? Reason, DateOnly EffectiveDate)
    : ICommand<Result>;

internal sealed class SuspendEmploymentCommandHandler : IRequestHandler<SuspendEmploymentCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SuspendEmploymentCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SuspendEmploymentCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.Suspend(request.Reason, request.EffectiveDate, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Returns Operational Status to Active from Suspended or Seconded.
/// commands.md lists this both as ReinstateEmploymentCommand (Career Movement
/// Commands) and ResumeEmploymentCommand (Separation Commands); both names describe
/// the identical <see cref="Domain.Employment.Reinstate"/> operation, so only one
/// command class exists here rather than two identical ones.
/// </summary>
public sealed record ReinstateEmploymentCommand(Guid EmploymentId, Guid TenantId, DateOnly EffectiveDate) : ICommand<Result>;

internal sealed class ReinstateEmploymentCommandHandler : IRequestHandler<ReinstateEmploymentCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReinstateEmploymentCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReinstateEmploymentCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.Reinstate(request.EffectiveDate, _timeProvider.GetUtcNow());
    }
}

public sealed record SecondEmploymentCommand(Guid EmploymentId, Guid TenantId, DateOnly EffectiveDate) : ICommand<Result>;

internal sealed class SecondEmploymentCommandHandler : IRequestHandler<SecondEmploymentCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SecondEmploymentCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SecondEmploymentCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.Second(request.EffectiveDate, _timeProvider.GetUtcNow());
    }
}

/// <summary>
/// Reaches the terminal Separated lifecycle stage. commands.md lists
/// TerminateEmploymentCommand, RetireEmploymentCommand, and SeparateEmploymentCommand
/// separately; all three call the identical <see cref="Domain.Employment.Separate"/>
/// method differing only in <see cref="SeparationType"/>, so one command carrying
/// that value replaces three otherwise-identical command classes.
/// </summary>
public sealed record SeparateEmploymentCommand(
    Guid EmploymentId,
    Guid TenantId,
    SeparationType SeparationType,
    string? TerminationReason,
    DateOnly LastWorkingDate,
    DateOnly EffectiveSeparationDate) : ICommand<Result>;

internal sealed class SeparateEmploymentCommandHandler : IRequestHandler<SeparateEmploymentCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public SeparateEmploymentCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(SeparateEmploymentCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.Separate(
                request.SeparationType, request.TerminationReason, request.LastWorkingDate,
                request.EffectiveSeparationDate, _timeProvider.GetUtcNow());
    }
}
