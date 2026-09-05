using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Application.Dtos;
using Hris.Foundation.Notification.Application.Mapping;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Queries;

public sealed record ListNotificationTemplatesQuery(Guid TenantId) : IQuery<Result<IReadOnlyList<NotificationTemplateDto>>>;

internal sealed class ListNotificationTemplatesQueryHandler
    : IRequestHandler<ListNotificationTemplatesQuery, Result<IReadOnlyList<NotificationTemplateDto>>>
{
    private readonly INotificationTemplateRepository _repository;

    public ListNotificationTemplatesQueryHandler(INotificationTemplateRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyList<NotificationTemplateDto>>> Handle(
        ListNotificationTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await _repository.ListByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return Result.Success<IReadOnlyList<NotificationTemplateDto>>(templates.Select(NotificationMapper.ToDto).ToList());
    }
}
