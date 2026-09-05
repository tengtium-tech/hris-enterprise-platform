using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Application.Dtos;
using Hris.Foundation.Notification.Application.Mapping;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Queries;

public sealed record GetNotificationQuery(Guid NotificationId) : IQuery<Result<NotificationDto>>;

internal sealed class GetNotificationQueryHandler : IRequestHandler<GetNotificationQuery, Result<NotificationDto>>
{
    private readonly INotificationRepository _repository;

    public GetNotificationQueryHandler(INotificationRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<NotificationDto>> Handle(GetNotificationQuery request, CancellationToken cancellationToken)
    {
        var notification = await _repository.GetByIdAsync(
            new NotificationId(request.NotificationId), cancellationToken).ConfigureAwait(false);

        return notification is null
            ? Result.Failure<NotificationDto>(NotificationErrors.NotificationNotFound)
            : Result.Success(NotificationMapper.ToDto(notification));
    }
}
