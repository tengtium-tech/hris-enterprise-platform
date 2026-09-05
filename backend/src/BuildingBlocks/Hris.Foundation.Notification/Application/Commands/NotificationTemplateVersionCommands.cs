using Hris.Application.Abstractions;
using Hris.Foundation.Notification.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Notification.Application.Commands;

public sealed record CreateNewNotificationTemplateDraftVersionCommand(
    Guid NotificationTemplateId,
    string Locale,
    string? Subject,
    string Body,
    IReadOnlyList<NotificationChannel> SupportedChannels) : ICommand<Result<int>>;

public sealed record PublishNotificationTemplateVersionCommand(
    Guid NotificationTemplateId,
    int VersionNumber) : ICommand<Result>;

public sealed record DeprecateNotificationTemplateVersionCommand(
    Guid NotificationTemplateId,
    int VersionNumber) : ICommand<Result>;

internal sealed class CreateNewNotificationTemplateDraftVersionCommandHandler
    : IRequestHandler<CreateNewNotificationTemplateDraftVersionCommand, Result<int>>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateNewNotificationTemplateDraftVersionCommandHandler(INotificationTemplateRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<int>> Handle(CreateNewNotificationTemplateDraftVersionCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(
            new NotificationTemplateId(request.NotificationTemplateId), cancellationToken).ConfigureAwait(false);
        if (template is null)
        {
            return Result.Failure<int>(NotificationErrors.TemplateNotFound);
        }

        var result = template.CreateNewDraftVersion(request.Locale, request.Subject, request.Body, request.SupportedChannels, _timeProvider.GetUtcNow());
        return result.IsFailure
            ? Result.Failure<int>(result.Error)
            : Result.Success(result.Value.VersionNumber);
    }
}

/// <summary>
/// No <c>UpdateAsync</c> call in either handler below: a template loaded through
/// <see cref="INotificationTemplateRepository.GetByIdAsync"/> is already tracked by the
/// same <c>HrisDbContext</c>, so the caller's own <c>TransactionBehavior</c> persists
/// the mutation via change tracking alone.
/// </summary>
internal sealed class PublishNotificationTemplateVersionCommandHandler : IRequestHandler<PublishNotificationTemplateVersionCommand, Result>
{
    private readonly INotificationTemplateRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishNotificationTemplateVersionCommandHandler(INotificationTemplateRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishNotificationTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(
            new NotificationTemplateId(request.NotificationTemplateId), cancellationToken).ConfigureAwait(false);

        return template is null
            ? Result.Failure(NotificationErrors.TemplateNotFound)
            : template.PublishVersion(request.VersionNumber, _timeProvider.GetUtcNow());
    }
}

internal sealed class DeprecateNotificationTemplateVersionCommandHandler : IRequestHandler<DeprecateNotificationTemplateVersionCommand, Result>
{
    private readonly INotificationTemplateRepository _repository;

    public DeprecateNotificationTemplateVersionCommandHandler(INotificationTemplateRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(DeprecateNotificationTemplateVersionCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetByIdAsync(
            new NotificationTemplateId(request.NotificationTemplateId), cancellationToken).ConfigureAwait(false);

        return template is null
            ? Result.Failure(NotificationErrors.TemplateNotFound)
            : template.DeprecateVersion(request.VersionNumber);
    }
}
