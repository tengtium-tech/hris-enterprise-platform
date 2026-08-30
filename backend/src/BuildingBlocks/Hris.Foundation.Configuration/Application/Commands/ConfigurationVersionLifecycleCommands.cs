using Hris.Application.Abstractions;
using Hris.Foundation.Configuration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Configuration.Application.Commands;

/// <summary>
/// The five remaining Configuration Lifecycle transitions
/// (configuration-framework.md: "Draft -&gt; Validated -&gt; Approved -&gt; Published
/// -&gt; Active -&gt; Deprecated -&gt; Archived") not already covered by
/// <see cref="CreateConfigurationSettingCommand"/> (which creates the first Draft) and
/// <see cref="CreateNewDraftVersionCommand"/> (which drafts the next one).
///
/// Grouped into one file as a stated deviation from this repository's usual
/// one-concept-per-file convention (CLAUDE.md, "Propagate by grep, not by list" section's
/// sibling principle of stating deviations rather than making them silently): each
/// command/handler pair below is a mechanical, near-identical wrapper -- load the
/// Aggregate by id, call the one <see cref="ConfigurationSetting"/> method that already
/// enforces the transition's invariants, translate the <see cref="Result"/> -- and the
/// five together are shorter to read as one file than as five files each restating the
/// same three-line shape.
/// </summary>
public sealed record ValidateConfigurationVersionCommand(
    Guid ConfigurationSettingId,
    Guid ConfigurationVersionId) : ICommand<Result>;

public sealed record ApproveConfigurationVersionCommand(
    Guid ConfigurationSettingId,
    Guid ConfigurationVersionId,
    Guid ApproverId) : ICommand<Result>;

public sealed record PublishConfigurationVersionCommand(
    Guid ConfigurationSettingId,
    Guid ConfigurationVersionId) : ICommand<Result>;

public sealed record ActivateConfigurationVersionCommand(
    Guid ConfigurationSettingId,
    Guid ConfigurationVersionId,
    DateOnly AsOfDate) : ICommand<Result>;

public sealed record DeprecateConfigurationVersionCommand(
    Guid ConfigurationSettingId,
    Guid ConfigurationVersionId) : ICommand<Result>;

public sealed record ArchiveConfigurationVersionCommand(
    Guid ConfigurationSettingId,
    Guid ConfigurationVersionId) : ICommand<Result>;

internal sealed class ValidateConfigurationVersionCommandHandler
    : IRequestHandler<ValidateConfigurationVersionCommand, Result>
{
    private readonly IConfigurationSettingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ValidateConfigurationVersionCommandHandler(IConfigurationSettingRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ValidateConfigurationVersionCommand request, CancellationToken cancellationToken)
    {
        var setting = await _repository
            .GetByIdAsync(new ConfigurationId(request.ConfigurationSettingId), cancellationToken)
            .ConfigureAwait(false);

        return setting is null
            ? Result.Failure(ConfigurationErrors.SettingNotFound)
            : setting.ValidateVersion(new ConfigurationVersionId(request.ConfigurationVersionId), _timeProvider.GetUtcNow());
    }
}

internal sealed class ApproveConfigurationVersionCommandHandler
    : IRequestHandler<ApproveConfigurationVersionCommand, Result>
{
    private readonly IConfigurationSettingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveConfigurationVersionCommandHandler(IConfigurationSettingRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveConfigurationVersionCommand request, CancellationToken cancellationToken)
    {
        var setting = await _repository
            .GetByIdAsync(new ConfigurationId(request.ConfigurationSettingId), cancellationToken)
            .ConfigureAwait(false);

        return setting is null
            ? Result.Failure(ConfigurationErrors.SettingNotFound)
            : setting.ApproveVersion(new ConfigurationVersionId(request.ConfigurationVersionId), request.ApproverId, _timeProvider.GetUtcNow());
    }
}

internal sealed class PublishConfigurationVersionCommandHandler
    : IRequestHandler<PublishConfigurationVersionCommand, Result>
{
    private readonly IConfigurationSettingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishConfigurationVersionCommandHandler(IConfigurationSettingRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishConfigurationVersionCommand request, CancellationToken cancellationToken)
    {
        var setting = await _repository
            .GetByIdAsync(new ConfigurationId(request.ConfigurationSettingId), cancellationToken)
            .ConfigureAwait(false);

        return setting is null
            ? Result.Failure(ConfigurationErrors.SettingNotFound)
            : setting.PublishVersion(new ConfigurationVersionId(request.ConfigurationVersionId), _timeProvider.GetUtcNow());
    }
}

internal sealed class ActivateConfigurationVersionCommandHandler
    : IRequestHandler<ActivateConfigurationVersionCommand, Result>
{
    private readonly IConfigurationSettingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ActivateConfigurationVersionCommandHandler(IConfigurationSettingRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ActivateConfigurationVersionCommand request, CancellationToken cancellationToken)
    {
        var setting = await _repository
            .GetByIdAsync(new ConfigurationId(request.ConfigurationSettingId), cancellationToken)
            .ConfigureAwait(false);

        return setting is null
            ? Result.Failure(ConfigurationErrors.SettingNotFound)
            : setting.ActivateVersion(new ConfigurationVersionId(request.ConfigurationVersionId), request.AsOfDate, _timeProvider.GetUtcNow());
    }
}

internal sealed class DeprecateConfigurationVersionCommandHandler
    : IRequestHandler<DeprecateConfigurationVersionCommand, Result>
{
    private readonly IConfigurationSettingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeprecateConfigurationVersionCommandHandler(IConfigurationSettingRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeprecateConfigurationVersionCommand request, CancellationToken cancellationToken)
    {
        var setting = await _repository
            .GetByIdAsync(new ConfigurationId(request.ConfigurationSettingId), cancellationToken)
            .ConfigureAwait(false);

        return setting is null
            ? Result.Failure(ConfigurationErrors.SettingNotFound)
            : setting.DeprecateVersion(new ConfigurationVersionId(request.ConfigurationVersionId), _timeProvider.GetUtcNow());
    }
}

internal sealed class ArchiveConfigurationVersionCommandHandler
    : IRequestHandler<ArchiveConfigurationVersionCommand, Result>
{
    private readonly IConfigurationSettingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveConfigurationVersionCommandHandler(IConfigurationSettingRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveConfigurationVersionCommand request, CancellationToken cancellationToken)
    {
        var setting = await _repository
            .GetByIdAsync(new ConfigurationId(request.ConfigurationSettingId), cancellationToken)
            .ConfigureAwait(false);

        return setting is null
            ? Result.Failure(ConfigurationErrors.SettingNotFound)
            : setting.ArchiveVersion(new ConfigurationVersionId(request.ConfigurationVersionId), _timeProvider.GetUtcNow());
    }
}
