using Hris.Application.Abstractions;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Commands;

/// <summary>
/// Position Aggregate's own reporting-hierarchy commands. Both handlers compute the
/// three cross-aggregate booleans <see cref="Domain.Position.AssignReportingPosition"/>
/// needs (existence, active status, would-create-a-cycle) here, before calling into
/// the Aggregate -- see that method's own remarks for why this cannot be done inside
/// the Aggregate itself.
/// </summary>
public sealed record AssignReportingPositionCommand(Guid PositionId, Guid TenantId, Guid ReportingPositionId) : ICommand<Result>;

internal sealed class AssignReportingPositionCommandHandler : IRequestHandler<AssignReportingPositionCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public AssignReportingPositionCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(AssignReportingPositionCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (positionResult.IsFailure)
        {
            return Result.Failure(positionResult.Error);
        }

        var reportingPosition = await _repository.GetByIdAsync(
            new PositionId(request.ReportingPositionId), cancellationToken).ConfigureAwait(false);
        var reportingPositionExists = reportingPosition is not null && reportingPosition.TenantId == request.TenantId;
        var reportingPositionIsActive = reportingPositionExists && reportingPosition!.Status == PositionStatus.Active;

        var wouldCreateCircularReporting = reportingPositionExists
            && await _repository.WouldCreateCircularReportingAsync(
                request.TenantId, request.PositionId, request.ReportingPositionId, cancellationToken).ConfigureAwait(false);

        return positionResult.Value.AssignReportingPosition(
            request.ReportingPositionId, reportingPositionExists, reportingPositionIsActive,
            wouldCreateCircularReporting, _timeProvider.GetUtcNow());
    }
}

public sealed record RemoveReportingPositionCommand(Guid PositionId, Guid TenantId) : ICommand<Result>;

internal sealed class RemoveReportingPositionCommandHandler : IRequestHandler<RemoveReportingPositionCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RemoveReportingPositionCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RemoveReportingPositionCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return positionResult.IsFailure
            ? Result.Failure(positionResult.Error)
            : positionResult.Value.RemoveReportingPosition(_timeProvider.GetUtcNow());
    }
}
