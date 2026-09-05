using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Commands;

public sealed record CreateNotificationTemplateCommand(
    Guid TenantId,
    string TemplateKey,
    NotificationType NotificationType,
    string Locale,
    string? Subject,
    string Body,
    IReadOnlyList<NotificationChannel> SupportedChannels) : ICommand<Result<Guid>>;

internal sealed class CreateNotificationTemplateCommandHandler : IRequestHandler<CreateNotificationTemplateCommand, Result<Guid>>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateNotificationTemplateCommandHandler(INotificationTemplateRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateNotificationTemplateCommand request, CancellationToken cancellationToken)
    {
        if (await _repository.ExistsByTemplateKeyAsync(request.TenantId, request.TemplateKey, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(NotificationErrors.DuplicateTemplateKey);
        }

        var createResult = NotificationTemplate.Create(
            request.TenantId, request.TemplateKey, request.NotificationType, request.Locale, request.Subject,
            request.Body, request.SupportedChannels, _timeProvider.GetUtcNow());
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        await _repository.AddAsync(createResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(createResult.Value.Id.Value);
    }
}
