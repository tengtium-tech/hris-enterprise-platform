using Hris.Application.Abstractions;
using Hris.Foundation.Configuration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Configuration.Application.Commands;

/// <summary>
/// Creates a new <see cref="ConfigurationSetting"/> with its first Draft version, per
/// configuration-framework.md's Configuration Lifecycle ("Draft -&gt; Validated -&gt;
/// ..."). One command per docs/08-devops/coding-standards.md's Application Layer
/// convention: "Commands and Queries are immutable records."
///
/// Carries raw primitives, not Domain Value Objects, across the MediatR boundary --
/// <see cref="CreateConfigurationSettingCommandHandler"/> is the one place a malformed
/// key or scope becomes a <see cref="ConfigurationErrors"/> failure, so a caller never
/// needs to construct a <see cref="ConfigurationKey"/> itself just to issue a command.
/// </summary>
public sealed record CreateConfigurationSettingCommand(
    string Key,
    ConfigurationScopeLevel ScopeLevel,
    Guid? ScopeId,
    ConfigurationCategory Category,
    ConfigurationDataType DataType,
    string InitialValue,
    DateOnly EffectiveDate,
    DateOnly? ExpirationDate,
    string ChangeSummary,
    Guid CreatedByUserId) : ICommand<Result<Guid>>;

/// <summary>
/// Handler is the only place this command's effects occur (coding-standards.md,
/// Application Layer). Validation of the command's own shape (required fields) runs
/// beforehand via <c>CreateConfigurationSettingCommandValidator</c>, per that same
/// section's "Validation... run before the handler executes, not interleaved with
/// handler logic" -- this handler only translates already-shape-valid input into Value
/// Objects and lets the Aggregate Root enforce its own business invariants.
/// </summary>
internal sealed class CreateConfigurationSettingCommandHandler
    : IRequestHandler<CreateConfigurationSettingCommand, Result<Guid>>
{
    private readonly IConfigurationSettingRepository _repository;
    private readonly TimeProvider _timeProvider;

    public CreateConfigurationSettingCommandHandler(
        IConfigurationSettingRepository repository,
        TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(CreateConfigurationSettingCommand request, CancellationToken cancellationToken)
    {
        var keyResult = ConfigurationKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<Guid>(keyResult.Error);
        }

        var scopeResult = ConfigurationScope.Create(request.ScopeLevel, request.ScopeId);
        if (scopeResult.IsFailure)
        {
            return Result.Failure<Guid>(scopeResult.Error);
        }

        // configuration-framework.md's Configuration Principles: "Single Source of
        // Truth." Two settings sharing a key and scope would make "the" value for that
        // key at that scope ambiguous -- rejected here, before ever constructing the
        // Aggregate, rather than left as a possible duplicate for a later query to
        // stumble over.
        if (await _repository.ExistsAsync(keyResult.Value, scopeResult.Value, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<Guid>(ConfigurationErrors.SettingAlreadyExistsForKeyAndScope);
        }

        var settingResult = ConfigurationSetting.Create(
            keyResult.Value,
            scopeResult.Value,
            request.Category,
            request.DataType,
            request.InitialValue,
            request.EffectiveDate,
            request.ExpirationDate,
            request.ChangeSummary,
            request.CreatedByUserId,
            _timeProvider.GetUtcNow());

        if (settingResult.IsFailure)
        {
            return Result.Failure<Guid>(settingResult.Error);
        }

        await _repository.AddAsync(settingResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(settingResult.Value.Id.Value);
    }
}
