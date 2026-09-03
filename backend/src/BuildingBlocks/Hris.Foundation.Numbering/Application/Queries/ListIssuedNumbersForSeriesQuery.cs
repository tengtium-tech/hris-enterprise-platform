using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Application.Dtos;
using Hris.Foundation.Numbering.Application.Mapping;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Queries;

public sealed record ListIssuedNumbersForSeriesQuery(Guid NumberSeriesId) : IQuery<Result<IReadOnlyCollection<IssuedNumberDto>>>;

internal sealed class ListIssuedNumbersForSeriesQueryHandler
    : IRequestHandler<ListIssuedNumbersForSeriesQuery, Result<IReadOnlyCollection<IssuedNumberDto>>>
{
    private readonly IIssuedNumberRepository _repository;

    public ListIssuedNumbersForSeriesQueryHandler(IIssuedNumberRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyCollection<IssuedNumberDto>>> Handle(
        ListIssuedNumbersForSeriesQuery request,
        CancellationToken cancellationToken)
    {
        var issuedNumbers = await _repository
            .GetBySeriesIdAsync(new NumberSeriesId(request.NumberSeriesId), cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyCollection<IssuedNumberDto> dtos = issuedNumbers.Select(NumberingMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
