using Hris.Application.Abstractions;
using Hris.Modules.Position.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Position.Application.Commands;

public sealed record UpdateAuthorizedHeadcountCommand(Guid PositionId, Guid TenantId, int AuthorizedHeadcount) : ICommand<Result>;

internal sealed class UpdateAuthorizedHeadcountCommandHandler : IRequestHandler<UpdateAuthorizedHeadcountCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public UpdateAuthorizedHeadcountCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(UpdateAuthorizedHeadcountCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return positionResult.IsFailure
            ? Result.Failure(positionResult.Error)
            : positionResult.Value.UpdateAuthorizedHeadcount(request.AuthorizedHeadcount, _timeProvider.GetUtcNow());
    }
}

public sealed record MarkPositionVacantCommand(Guid PositionId, Guid TenantId) : ICommand<Result>;

internal sealed class MarkPositionVacantCommandHandler : IRequestHandler<MarkPositionVacantCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MarkPositionVacantCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MarkPositionVacantCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return positionResult.IsFailure
            ? Result.Failure(positionResult.Error)
            : positionResult.Value.MarkVacant(_timeProvider.GetUtcNow());
    }
}

public sealed record MarkPositionFilledCommand(Guid PositionId, Guid TenantId) : ICommand<Result>;

internal sealed class MarkPositionFilledCommandHandler : IRequestHandler<MarkPositionFilledCommand, Result>
{
    private readonly IPositionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public MarkPositionFilledCommandHandler(IPositionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(MarkPositionFilledCommand request, CancellationToken cancellationToken)
    {
        var positionResult = await PositionLookup.LoadPositionForTenantAsync(
            _repository, request.PositionId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return positionResult.IsFailure
            ? Result.Failure(positionResult.Error)
            : positionResult.Value.MarkFilled(_timeProvider.GetUtcNow());
    }
}
