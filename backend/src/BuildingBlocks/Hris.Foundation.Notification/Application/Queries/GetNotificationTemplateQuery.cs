using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Application.Dtos;
using Hris.Foundation.Notification.Application.Mapping;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Queries;

public sealed record GetNotificationTemplateQuery(Guid NotificationTemplateId) : IQuery<Result<NotificationTemplateDto>>;

internal sealed class GetNotificationTemplateQueryHandler : IRequestHandler<GetNotificationTemplateQuery, Result<NotificationTemplateDto>>
{
    private readonly INotificationTemplateRepository _repository;

    public GetNotificationTemplateQueryHandler(INotificationTemplateRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<NotificationTemplateDto>> Handle(GetNotificationTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(
            new NotificationTemplateId(request.NotificationTemplateId), cancellationToken).ConfigureAwait(false);

        return template is null
            ? Result.Failure<NotificationTemplateDto>(NotificationErrors.TemplateNotFound)
            : Result.Success(NotificationMapper.ToDto(template));
    }
}
