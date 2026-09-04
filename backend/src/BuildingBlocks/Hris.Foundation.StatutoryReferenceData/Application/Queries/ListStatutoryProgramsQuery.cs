using Hris.Application.Abstractions;
using Hris.Foundation.StatutoryReferenceData.Application.Dtos;
using Hris.Foundation.StatutoryReferenceData.Application.Mapping;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.StatutoryReferenceData.Application.Queries;

public sealed record ListStatutoryProgramsQuery(string Country) : IQuery<Result<IReadOnlyList<StatutoryProgramDto>>>;

internal sealed class ListStatutoryProgramsQueryHandler
    : IRequestHandler<ListStatutoryProgramsQuery, Result<IReadOnlyList<StatutoryProgramDto>>>
{
    private readonly IStatutoryProgramRepository _repository;

    public ListStatutoryProgramsQueryHandler(IStatutoryProgramRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<StatutoryProgramDto>>> Handle(
        ListStatutoryProgramsQuery request, CancellationToken cancellationToken)
    {
        var countryResult = StatutoryCountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<StatutoryProgramDto>>(countryResult.Error);
        }

        var programs = await _repository.ListByCountryAsync(countryResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<StatutoryProgramDto>>(
            programs.Select(StatutoryReferenceDataMapper.ToDto).ToList());
    }
}
