using Hris.Application.Abstractions;
using Hris.Modules.Employment.Application.Dtos;
using Hris.Modules.Employment.Application.Mapping;
using Hris.Modules.Employment.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Employment.Application.Queries;

public sealed record GetEmploymentContractQuery(Guid EmploymentContractId, Guid TenantId)
    : IQuery<Result<EmploymentContractDto>>;

internal sealed class GetEmploymentContractQueryHandler
    : IRequestHandler<GetEmploymentContractQuery, Result<EmploymentContractDto>>
{
    private readonly IEmploymentContractRepository _repository;

    public GetEmploymentContractQueryHandler(IEmploymentContractRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<EmploymentContractDto>> Handle(
        GetEmploymentContractQuery request, CancellationToken cancellationToken)
    {
        var contractResult = await EmploymentLookup.LoadContractForTenantAsync(
            _repository, request.EmploymentContractId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return contractResult.IsFailure
            ? Result.Failure<EmploymentContractDto>(contractResult.Error)
            : Result.Success(EmploymentMapper.ToDto(contractResult.Value));
    }
}

public sealed record ListEmploymentContractsQuery(Guid TenantId, Guid EmploymentId)
    : IQuery<Result<IReadOnlyList<EmploymentContractDto>>>;

internal sealed class ListEmploymentContractsQueryHandler
    : IRequestHandler<ListEmploymentContractsQuery, Result<IReadOnlyList<EmploymentContractDto>>>
{
    private readonly IEmploymentContractRepository _repository;

    public ListEmploymentContractsQueryHandler(IEmploymentContractRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<EmploymentContractDto>>> Handle(
        ListEmploymentContractsQuery request, CancellationToken cancellationToken)
    {
        var contracts = await _repository
            .ListByEmploymentIdAsync(request.TenantId, request.EmploymentId, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<EmploymentContractDto> dtos = contracts.Select(EmploymentMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
