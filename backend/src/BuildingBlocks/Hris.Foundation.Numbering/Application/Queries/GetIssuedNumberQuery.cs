using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Application.Dtos;
using Hris.Foundation.Numbering.Application.Mapping;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Queries;

public sealed record GetIssuedNumberQuery(Guid IssuedNumberId) : IQuery<Result<IssuedNumberDto>>;

internal sealed class GetIssuedNumberQueryHandler : IRequestHandler<GetIssuedNumberQuery, Result<IssuedNumberDto>>
{
    private readonly IIssuedNumberRepository _repository;

    public GetIssuedNumberQueryHandler(IIssuedNumberRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IssuedNumberDto>> Handle(GetIssuedNumberQuery request, CancellationToken cancellationToken)
    {
        var issuedNumber = await _repository.GetByIdAsync(new IssuedNumberId(request.IssuedNumberId), cancellationToken).ConfigureAwait(false);

        return issuedNumber is null
            ? Result.Failure<IssuedNumberDto>(NumberingErrors.IssuedNumberNotFound)
            : Result.Success(NumberingMapper.ToDto(issuedNumber));
    }
}
