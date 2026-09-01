using Hris.Application.Abstractions;
using Hris.Foundation.Events.Application.Dtos;
using Hris.Foundation.Events.Application.Mapping;
using Hris.Foundation.Events.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Events.Application.Queries;

/// <summary>
/// Dead Letter Queue's own "DLQ Monitoring" capability. <see cref="TenantId"/> is
/// optional -- see <see cref="IOutboxEntryRepository.GetDeadLetteredAsync"/>'s own
/// remarks for why a <c>null</c> value is a deliberate cross-tenant operator view
/// rather than an oversight, and why no Authorization check gates that view yet.
/// </summary>
public sealed record GetDeadLetterEventsQuery(Guid? TenantId, int MaxResults) : IQuery<Result<IReadOnlyList<OutboxEntryDto>>>;

internal sealed class GetDeadLetterEventsQueryHandler
    : IRequestHandler<GetDeadLetterEventsQuery, Result<IReadOnlyList<OutboxEntryDto>>>
{
    private readonly IOutboxEntryRepository _repository;

    public GetDeadLetterEventsQueryHandler(IOutboxEntryRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<OutboxEntryDto>>> Handle(
        GetDeadLetterEventsQuery request, CancellationToken cancellationToken)
    {
        var entries = await _repository
            .GetDeadLetteredAsync(request.TenantId, request.MaxResults, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<OutboxEntryDto> dtos = entries.Select(entry => entry.ToDto()).ToList();

        return Result.Success(dtos);
    }
}
