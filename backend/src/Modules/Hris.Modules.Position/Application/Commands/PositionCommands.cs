using Hris.Application.Abstractions;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Commands;

public sealed record CreatePositionCommand(
    Guid TenantId,
    string? Number,
    string? Title,
    string? PositionType,
    Guid OrganizationId,
    Guid? LegalEntityId,
    Guid? BusinessUnitId,
    Guid? DivisionId,
    Guid? DepartmentId,
    Guid? SectionId,
    Guid? TeamId,
    Guid? WorkLocationId,
    Guid? CostCenterId,
    Guid JobFamilyId,
    Guid JobClassificationId,
    Guid JobGradeId,
    Guid? ReportingPositionId,
    int AuthorizedHeadcount) : ICommand<Result<Guid>>;

internal sealed class CreatePositionCommandHandler : IRequestHandler<CreatePositionCommand, Result<Guid>>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreatePositionCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreatePositionCommand request, CancellationToken cancellationToken)
    {
        if (request.Number is not null
            && await _repository.ExistsWithNumberAsync(request.TenantId, request.Number, null, cancellationToken)
                .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(PositionErrors.DuplicatePositionNumber);
        }

        var id = new PositionId(Guid.NewGuid());
        var nowUtc = _timeProvider.GetUtcNow();

        var positionResult = Domain.Position.Create(
            id, request.TenantId, request.Number, request.Title, request.PositionType, request.OrganizationId,
            request.LegalEntityId, request.BusinessUnitId, request.DivisionId, request.DepartmentId, request.SectionId,
            request.TeamId, request.WorkLocationId, request.CostCenterId, request.JobFamilyId,
            request.JobClassificationId, request.JobGradeId, request.ReportingPositionId, request.AuthorizedHeadcount,
            nowUtc);
        if (positionResult.IsFailure)
        {
            return Result.Failure<Guid>(positionResult.Error);
        }

        await _repository.AddAsync(positionResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(positionResult.Value.Id.Value);
    }
}

public sealed record UpdatePositionCommand(
    Guid PositionId,
    Guid TenantId,
    string? Title,
    string? PositionType,
    Guid OrganizationId,
    Guid? LegalEntityId,
    Guid? BusinessUnitId,
    Guid? DivisionId,
    Guid? DepartmentId,
    Guid? SectionId,
    Guid? TeamId,
    Guid? WorkLocationId,
    Guid? CostCenterId,
    Guid JobFamilyId,
    Guid JobClassificationId,
    Guid JobGradeId) : ICommand<Result>;

internal sealed class UpdatePositionCommandHandler : IRequestHandler<UpdatePositionCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdatePositionCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdatePositionCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);
        if (positionResult.IsFailure)
        {
            return Result.Failure(positionResult.Error);
        }

        return positionResult.Value.Update(
            request.Title, request.PositionType, request.OrganizationId, request.LegalEntityId, request.BusinessUnitId,
            request.DivisionId, request.DepartmentId, request.SectionId, request.TeamId, request.WorkLocationId,
            request.CostCenterId, request.JobFamilyId, request.JobClassificationId, request.JobGradeId,
            _timeProvider.GetUtcNow());
    }
}

public sealed record ActivatePositionCommand(Guid PositionId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivatePositionCommandHandler : IRequestHandler<ActivatePositionCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivatePositionCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivatePositionCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return positionResult.IsFailure
            ? Result.Failure(positionResult.Error)
            : positionResult.Value.Activate(_timeProvider.GetUtcNow());
    }
}

public sealed record DeactivatePositionCommand(Guid PositionId, Guid TenantId) : ICommand<Result>;

internal sealed class DeactivatePositionCommandHandler : IRequestHandler<DeactivatePositionCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeactivatePositionCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeactivatePositionCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return positionResult.IsFailure
            ? Result.Failure(positionResult.Error)
            : positionResult.Value.Deactivate(_timeProvider.GetUtcNow());
    }
}

public sealed record ArchivePositionCommand(Guid PositionId, Guid TenantId) : ICommand<Result>;

internal sealed class ArchivePositionCommandHandler : IRequestHandler<ArchivePositionCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchivePositionCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchivePositionCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return positionResult.IsFailure
            ? Result.Failure(positionResult.Error)
            : positionResult.Value.Archive(_timeProvider.GetUtcNow());
    }
}
