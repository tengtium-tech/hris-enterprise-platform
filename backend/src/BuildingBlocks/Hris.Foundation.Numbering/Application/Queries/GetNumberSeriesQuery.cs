using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Application.Dtos;
using Hris.Foundation.Numbering.Application.Mapping;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Queries;

/// <summary>
/// Reads one number series back by its own stable <see cref="SeriesKey"/> -- the
/// natural key a caller requesting a number actually has in hand, matching
/// <c>GetExtensionPointQuery</c>'s own identical by-natural-key shape.
/// </summary>
public sealed record GetNumberSeriesQuery(string Key) : IQuery<Result<NumberSeriesDto>>;

internal sealed class GetNumberSeriesQueryHandler : IRequestHandler<GetNumberSeriesQuery, Result<NumberSeriesDto>>
{
    private readonly INumberSeriesRepository _repository;

    public GetNumberSeriesQueryHandler(INumberSeriesRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<NumberSeriesDto>> Handle(GetNumberSeriesQuery request, CancellationToken cancellationToken)
    {
        var keyResult = SeriesKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<NumberSeriesDto>(keyResult.Error);
        }

        var numberSeries = await _repository.GetByKeyAsync(keyResult.Value, cancellationToken).ConfigureAwait(false);

        return numberSeries is null
            ? Result.Failure<NumberSeriesDto>(NumberingErrors.NumberSeriesNotFound)
            : Result.Success(NumberingMapper.ToDto(numberSeries));
    }
}
