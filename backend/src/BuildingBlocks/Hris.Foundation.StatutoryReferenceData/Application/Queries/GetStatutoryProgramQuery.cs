using Hris.Application.Abstractions;
using Hris.Foundation.StatutoryReferenceData.Application.Dtos;
using Hris.Foundation.StatutoryReferenceData.Application.Mapping;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.StatutoryReferenceData.Application.Queries;

public sealed record GetStatutoryProgramQuery(Guid StatutoryProgramId) : IQuery<Result<StatutoryProgramDto>>;

internal sealed class GetStatutoryProgramQueryHandler : IRequestHandler<GetStatutoryProgramQuery, Result<StatutoryProgramDto>>
{
    private readonly IStatutoryProgramRepository _repository;

    public GetStatutoryProgramQueryHandler(IStatutoryProgramRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<StatutoryProgramDto>> Handle(GetStatutoryProgramQuery request, CancellationToken cancellationToken)
    {
        var program = await _repository.GetByIdAsync(
            new StatutoryProgramId(request.StatutoryProgramId), cancellationToken).ConfigureAwait(false);

        return program is null
            ? Result.Failure<StatutoryProgramDto>(StatutoryReferenceDataErrors.ProgramNotFound)
            : Result.Success(StatutoryReferenceDataMapper.ToDto(program));
    }
}
