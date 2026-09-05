using Hris.SharedKernel;

namespace Hris.Foundation.Notification.Domain;

/// <summary>
/// Aggregate Root holding one reusable message template and every
/// <see cref="NotificationTemplateVersion"/> ever drafted, published, or deprecated for
/// it, per notification-framework.md's own Message Templates section. The same
/// "config aggregate owns its own versioned child Entities" shape
/// <c>WorkflowDefinition</c>/<c>WorkflowDefinitionVersion</c> and
/// <c>ConfigurationSetting</c>/<c>ConfigurationVersion</c> already establish.
///
/// <see cref="TenantId"/> is a plain <see cref="Guid"/>, caller-supplied -- built
/// concretely, not deferred, since this document names no platform-owned-data
/// exception the way statutory-reference-data.md's own Security Considerations do for
/// itself; templates are ordinary tenant-configured content ("Template Approval
/// Workflow" in Security Considerations implies a tenant-level approval process, not a
/// platform-wide one).
/// </summary>
public sealed class NotificationTemplate : AggregateRoot<NotificationTemplateId>
{
    private readonly List<NotificationTemplateVersion> _versions = [];

    public Guid TenantId { get; }

    public string TemplateKey { get; }

    public NotificationType NotificationType { get; }

    public IReadOnlyList<NotificationTemplateVersion> Versions => _versions.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; }

    private NotificationTemplate(NotificationTemplateId id, Guid tenantId, string templateKey, NotificationType notificationType, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        TemplateKey = templateKey;
        NotificationType = notificationType;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Registers a new template with its own first <see cref="NotificationTemplateVersion"/>
    /// (v1, Draft). <paramref name="templateKey"/> uniqueness within
    /// <paramref name="tenantId"/> is checked by the caller before this factory runs,
    /// the same split every other uniqueness-checked factory in this codebase
    /// establishes. Raises no event of its own -- this document's own Domain Events
    /// list names <see cref="NotificationTemplateUpdated"/> only for a version actually
    /// publishing, not for registration.
    /// </summary>
    public static Result<NotificationTemplate> Create(
        Guid tenantId,
        string? templateKey,
        NotificationType notificationType,
        string locale,
        string? subject,
        string? body,
        IReadOnlyList<NotificationChannel> supportedChannels,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstDefault(tenantId, nameof(tenantId));

        if (string.IsNullOrWhiteSpace(templateKey))
        {
            return Result.Failure<NotificationTemplate>(NotificationErrors.TemplateKeyRequired);
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            return Result.Failure<NotificationTemplate>(NotificationErrors.LocaleRequired);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Result.Failure<NotificationTemplate>(NotificationErrors.BodyRequired);
        }

        if (supportedChannels is null || supportedChannels.Count == 0)
        {
            return Result.Failure<NotificationTemplate>(NotificationErrors.SupportedChannelsRequired);
        }

        var template = new NotificationTemplate(new NotificationTemplateId(Guid.NewGuid()), tenantId, templateKey.Trim(), notificationType, nowUtc);

        template._versions.Add(new NotificationTemplateVersion(
            new NotificationTemplateVersionId(Guid.NewGuid()), 1, locale.Trim(), subject, body.Trim(), supportedChannels, nowUtc));

        return Result.Success(template);
    }

    /// <summary>
    /// Drafts the next version's own content. Refuses while an unpublished draft
    /// already exists -- the identical guard <c>WorkflowDefinition.CreateNewDraftVersion</c>'s
    /// own remarks establish for itself.
    /// </summary>
    public Result<NotificationTemplateVersion> CreateNewDraftVersion(
        string locale, string? subject, string? body, IReadOnlyList<NotificationChannel> supportedChannels, DateTimeOffset nowUtc)
    {
        if (_versions.Any(v => v.Status == NotificationTemplateVersionStatus.Draft))
        {
            return Result.Failure<NotificationTemplateVersion>(NotificationErrors.DraftAlreadyExists);
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            return Result.Failure<NotificationTemplateVersion>(NotificationErrors.LocaleRequired);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            return Result.Failure<NotificationTemplateVersion>(NotificationErrors.BodyRequired);
        }

        if (supportedChannels is null || supportedChannels.Count == 0)
        {
            return Result.Failure<NotificationTemplateVersion>(NotificationErrors.SupportedChannelsRequired);
        }

        var version = new NotificationTemplateVersion(
            new NotificationTemplateVersionId(Guid.NewGuid()), _versions.Count + 1, locale.Trim(), subject, body.Trim(), supportedChannels, nowUtc);

        _versions.Add(version);
        return Result.Success(version);
    }

    /// <summary>
    /// Publishes the given Draft version. If a different version is currently
    /// <see cref="NotificationTemplateVersionStatus.Published"/>, it is deprecated in
    /// the same operation -- the identical within-aggregate supersession
    /// <c>WorkflowDefinition.PublishVersion</c>'s own remarks already establish. Raises
    /// <see cref="NotificationTemplateUpdated"/>, this document's own one named event
    /// for this aggregate.
    /// </summary>
    public Result PublishVersion(int versionNumber, DateTimeOffset nowUtc)
    {
        var version = FindVersion(versionNumber);
        if (version is null)
        {
            return Result.Failure(NotificationErrors.VersionNotFound);
        }

        var result = version.Publish(nowUtc);
        if (result.IsFailure)
        {
            return result;
        }

        var previouslyPublished = _versions.FirstOrDefault(
            v => v.VersionNumber != versionNumber && v.Status == NotificationTemplateVersionStatus.Published);
        previouslyPublished?.Deprecate();

        AddDomainEvent(new NotificationTemplateUpdated(Guid.NewGuid(), nowUtc, Id, versionNumber));
        return Result.Success();
    }

    public Result DeprecateVersion(int versionNumber)
    {
        var version = FindVersion(versionNumber);
        return version is null
            ? Result.Failure(NotificationErrors.VersionNotFound)
            : version.Deprecate();
    }

    public NotificationTemplateVersion? GetPublishedVersion() =>
        _versions.FirstOrDefault(v => v.Status == NotificationTemplateVersionStatus.Published);

    private NotificationTemplateVersion? FindVersion(int versionNumber) =>
        _versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
}
