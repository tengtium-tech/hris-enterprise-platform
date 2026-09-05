using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Application.Dtos;
using Hris.Foundation.Notification.Application.Mapping;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Queries;

public sealed record GetNotificationPreferenceQuery(Guid TenantId, Guid UserId) : IQuery<Result<NotificationPreferenceDto>>;

internal sealed class GetNotificationPreferenceQueryHandler
    : IRequestHandler<GetNotificationPreferenceQuery, Result<NotificationPreferenceDto>>
{
    private readonly INotificationPreferenceRepository _repository;

    public GetNotificationPreferenceQueryHandler(INotificationPreferenceRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<NotificationPreferenceDto>> Handle(GetNotificationPreferenceQuery request, CancellationToken cancellationToken)
    {
        var preference = await _repository.GetByUserAsync(request.TenantId, request.UserId, cancellationToken).ConfigureAwait(false);

        return preference is null
            ? Result.Failure<NotificationPreferenceDto>(NotificationErrors.PreferenceNotFound)
            : Result.Success(NotificationMapper.ToDto(preference));
    }
}
