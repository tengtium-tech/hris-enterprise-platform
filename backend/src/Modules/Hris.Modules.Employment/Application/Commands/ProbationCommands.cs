using Hris.Application.Abstractions;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Commands;

public sealed record StartProbationCommand(Guid EmploymentId, Guid TenantId, DateOnly StartDate, int DurationDays)
    : ICommand<Result>;

internal sealed class StartProbationCommandHandler : IRequestHandler<StartProbationCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public StartProbationCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(StartProbationCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.StartProbation(request.StartDate, request.DurationDays, _timeProvider.GetUtcNow());
    }
}

public sealed record ExtendProbationCommand(Guid EmploymentId, Guid TenantId, int AdditionalDurationDays) : ICommand<Result>;

internal sealed class ExtendProbationCommandHandler : IRequestHandler<ExtendProbationCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ExtendProbationCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ExtendProbationCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.ExtendProbation(request.AdditionalDurationDays, _timeProvider.GetUtcNow());
    }
}

public sealed record ConfirmEmploymentCommand(Guid EmploymentId, Guid TenantId, string? NewEmploymentType) : ICommand<Result>;

internal sealed class ConfirmEmploymentCommandHandler : IRequestHandler<ConfirmEmploymentCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ConfirmEmploymentCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ConfirmEmploymentCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.ConfirmEmployment(request.NewEmploymentType, _timeProvider.GetUtcNow());
    }
}

public sealed record FailProbationCommand(Guid EmploymentId, Guid TenantId, DateOnly LastWorkingDate, DateOnly EffectiveSeparationDate)
    : ICommand<Result>;

internal sealed class FailProbationCommandHandler : IRequestHandler<FailProbationCommand, Result>
{
    private readonly IEmploymentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public FailProbationCommandHandler(IEmploymentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(FailProbationCommand request, CancellationToken cancellationToken)
    {
        var employmentResult = await EmploymentLookup.LoadEmploymentForTenantAsync(
            _repository, request.EmploymentId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return employmentResult.IsFailure
            ? Result.Failure(employmentResult.Error)
            : employmentResult.Value.FailProbation(
                request.LastWorkingDate, request.EffectiveSeparationDate, _timeProvider.GetUtcNow());
    }
}
