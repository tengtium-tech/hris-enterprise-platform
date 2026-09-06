using Hris.Application.Abstractions;
using Hris.Foundation.Integration.Application.Dtos;
using Hris.Foundation.Integration.Application.Mapping;
using Hris.Foundation.Integration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Integration.Application.Queries;

public sealed record GetIntegrationRunQuery(Guid IntegrationRunId, Guid TenantId) : IQuery<Result<IntegrationRunDto>>;

internal sealed class GetIntegrationRunQueryHandler : IRequestHandler<GetIntegrationRunQuery, Result<IntegrationRunDto>>
{
    private readonly IIntegrationRunRepository _repository;

    public GetIntegrationRunQueryHandler(IIntegrationRunRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IntegrationRunDto>> Handle(GetIntegrationRunQuery request, CancellationToken cancellationToken)
    {
        var runResult = await IntegrationLookup.LoadIntegrationRunForTenantAsync(
            _repository, request.IntegrationRunId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return runResult.IsFailure
            ? Result.Failure<IntegrationRunDto>(runResult.Error)
            : Result.Success(IntegrationMapper.ToDto(runResult.Value));
    }
}
