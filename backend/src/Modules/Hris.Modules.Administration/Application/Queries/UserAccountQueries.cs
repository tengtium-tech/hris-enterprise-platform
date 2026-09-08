using Hris.Application.Abstractions;
using Hris.Modules.Administration.Application.Dtos;
using Hris.Modules.Administration.Application.Mapping;
using Hris.Modules.Administration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Modules.Administration.Application.Queries;

public sealed record GetUserAccountQuery(Guid UserAccountId, Guid TenantId) : IQuery<Result<UserAccountDto>>;

internal sealed class GetUserAccountQueryHandler : IRequestHandler<GetUserAccountQuery, Result<UserAccountDto>>
{
    private readonly IUserAccountRepository _repository;

    public GetUserAccountQueryHandler(IUserAccountRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<UserAccountDto>> Handle(GetUserAccountQuery request, CancellationToken cancellationToken)
    {
        var result = await AdministrationLookup.LoadUserAccountForTenantAsync(
            _repository, request.UserAccountId, request.TenantId, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result.Failure<UserAccountDto>(result.Error)
            : Result.Success(AdministrationMapper.ToDto(result.Value));
    }
}

public sealed record GetUserAccountByEmployeeQuery(Guid EmployeeId, Guid TenantId) : IQuery<Result<UserAccountDto>>;

internal sealed class GetUserAccountByEmployeeQueryHandler : IRequestHandler<GetUserAccountByEmployeeQuery, Result<UserAccountDto>>
{
    private readonly IUserAccountRepository _repository;

    public GetUserAccountByEmployeeQueryHandler(IUserAccountRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<UserAccountDto>> Handle(GetUserAccountByEmployeeQuery request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByEmployeeIdAsync(request.TenantId, request.EmployeeId, cancellationToken)
            .ConfigureAwait(false);

        return account is null
            ? Result.Failure<UserAccountDto>(AdministrationErrors.UserAccountNotFound)
            : Result.Success(AdministrationMapper.ToDto(account));
    }
}

public sealed record ListUserAccountsQuery(Guid TenantId, UserAccountStatus? StatusFilter) : IQuery<Result<IReadOnlyList<UserAccountSummaryDto>>>;

internal sealed class ListUserAccountsQueryHandler : IRequestHandler<ListUserAccountsQuery, Result<IReadOnlyList<UserAccountSummaryDto>>>
{
    private readonly IUserAccountRepository _repository;

    public ListUserAccountsQueryHandler(IUserAccountRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<UserAccountSummaryDto>>> Handle(ListUserAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        IEnumerable<UserAccount> filtered = accounts;
        if (request.StatusFilter.HasValue)
        {
            filtered = filtered.Where(account => account.Status == request.StatusFilter.Value);
        }

        IReadOnlyList<UserAccountSummaryDto> dtos = filtered.Select(AdministrationMapper.ToSummaryDto).ToList();
        return Result.Success(dtos);
    }
}
