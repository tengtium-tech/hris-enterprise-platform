using Hris.Application.Abstractions;
using Hris.Foundation.Configuration.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Application.Commands;

/// <summary>
/// Drafts the next <see cref="ConfigurationVersion"/> for an existing
/// <see cref="ConfigurationSetting"/>, per <see cref="ConfigurationSetting.CreateNewDraftVersion"/>.
/// </summary>
public sealed record CreateNewDraftVersionCommand(
    Guid ConfigurationSettingId,
    string Value,
    DateOnly EffectiveDate,
    DateOnly? ExpirationDate,
    string ChangeSummary,
    Guid CreatedByUserId) : ICommand<Result<Guid>>;

internal sealed class CreateNewDraftVersionCommandHandler
    : MediatR.IRequestHandler<CreateNewDraftVersionCommand, Result<Guid>>
{
    private readonly IConfigurationSettingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateNewDraftVersionCommandHandler(IConfigurationSettingRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateNewDraftVersionCommand request, CancellationToken cancellationToken)
    {
        var setting = await _repository
            .GetByIdAsync(new ConfigurationId(request.ConfigurationSettingId), cancellationToken)
            .ConfigureAwait(false);

        if (setting is null)
        {
            return Result.Failure<Guid>(ConfigurationErrors.SettingNotFound);
        }

        var result = setting.CreateNewDraftVersion(
            request.Value,
            request.EffectiveDate,
            request.ExpirationDate,
            request.ChangeSummary,
            request.CreatedByUserId,
            _timeProvider.GetUtcNow());

        return result.IsFailure
            ? Result.Failure<Guid>(result.Error)
            : Result.Success(result.Value.Id.Value);
    }
}
