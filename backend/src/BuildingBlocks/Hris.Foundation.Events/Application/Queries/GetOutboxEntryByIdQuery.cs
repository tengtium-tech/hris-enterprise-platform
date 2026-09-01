using Hris.Application.Abstractions;
using Hris.Foundation.Events.Application.Dtos;
using Hris.Foundation.Events.Application.Mapping;
using Hris.Foundation.Events.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Events.Application.Queries;

/// <summary>
/// Dead Letter Queue's own "Diagnostics" capability -- one outbox entry's full detail,
/// for an operator investigating a specific failed or dead-lettered event before
/// deciding whether to replay or requeue it.
/// </summary>
public sealed record GetOutboxEntryByIdQuery(Guid OutboxEntryId) : IQuery<Result<OutboxEntryDto>>;

internal sealed class GetOutboxEntryByIdQueryHandler : IRequestHandler<GetOutboxEntryByIdQuery, Result<OutboxEntryDto>>
{
    private readonly IOutboxEntryRepository _repository;

    public GetOutboxEntryByIdQueryHandler(IOutboxEntryRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<OutboxEntryDto>> Handle(GetOutboxEntryByIdQuery request, CancellationToken cancellationToken)
    {
        var entry = await _repository
            .GetByIdAsync(new OutboxEntryId(request.OutboxEntryId), cancellationToken)
            .ConfigureAwait(false);

        return entry is null
            ? Result.Failure<OutboxEntryDto>(EventErrors.OutboxEntryNotFound)
            : Result.Success(entry.ToDto());
    }
}
