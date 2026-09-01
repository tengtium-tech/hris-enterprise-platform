using Hris.Application.Abstractions;
using Hris.Foundation.Audit.Application.Dtos;
using Hris.Foundation.Audit.Application.Mapping;
using Hris.Foundation.Audit.Domain;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Audit.Application.Queries;

/// <summary>
/// Diagnostics/detail read for one <see cref="AuditRecord"/> -- the same
/// authorization-gated reasoning <see cref="SearchAuditRecordsQuery"/>'s own remarks
/// state applies here too.
/// </summary>
public sealed record GetAuditRecordByIdQuery(
    Guid RequestingPrincipalId,
    OrganizationalScopeLevel ScopeLevel,
    Guid ScopeId,
    Guid AuditRecordId) : IQuery<Result<AuditRecordDto>>;

internal sealed class GetAuditRecordByIdQueryHandler : IRequestHandler<GetAuditRecordByIdQuery, Result<AuditRecordDto>>
{
    private readonly IAuditRecordRepository _repository;
    private readonly ISender _sender;

    public GetAuditRecordByIdQueryHandler(IAuditRecordRepository repository, ISender sender)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
    }

    public async Task<Result<AuditRecordDto>> Handle(GetAuditRecordByIdQuery request, CancellationToken cancellationToken)
    {
        var authorizationResult = await _sender.Send(
            new CheckAuthorizationQuery(
                request.RequestingPrincipalId, "AuditRecord", PermissionAction.Read, request.ScopeLevel, request.ScopeId),
            cancellationToken).ConfigureAwait(false);

        if (authorizationResult.IsFailure)
        {
            return Result.Failure<AuditRecordDto>(authorizationResult.Error);
        }

        if (!authorizationResult.Value.IsAllowed)
        {
            return Result.Failure<AuditRecordDto>(AuditErrors.NotAuthorizedToAccessAuditRecords);
        }

        var record = await _repository
            .GetByIdAsync(new AuditRecordId(request.AuditRecordId), cancellationToken)
            .ConfigureAwait(false);

        return record is null
            ? Result.Failure<AuditRecordDto>(AuditErrors.AuditRecordNotFound)
            : Result.Success(record.ToDto());
    }
}
