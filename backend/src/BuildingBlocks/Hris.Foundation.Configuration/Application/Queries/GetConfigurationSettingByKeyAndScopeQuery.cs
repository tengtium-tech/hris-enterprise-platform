using Hris.Application.Abstractions;
using Hris.Foundation.Configuration.Application.Dtos;
using Hris.Foundation.Configuration.Application.Mapping;
using Hris.Foundation.Configuration.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Configuration.Application.Queries;

/// <summary>
/// Returns one <see cref="ConfigurationSetting"/> and every version it has ever had,
/// for administrative/audit display -- not for runtime value resolution, which is
/// <see cref="ResolveConfigurationValueQuery"/>'s job.
/// </summary>
public sealed record GetConfigurationSettingByKeyAndScopeQuery(
    string Key,
    ConfigurationScopeLevel ScopeLevel,
    Guid? ScopeId) : IQuery<Result<ConfigurationSettingDto>>;

internal sealed class GetConfigurationSettingByKeyAndScopeQueryHandler
    : IRequestHandler<GetConfigurationSettingByKeyAndScopeQuery, Result<ConfigurationSettingDto>>
{
    private readonly IConfigurationSettingRepository _repository;

    public GetConfigurationSettingByKeyAndScopeQueryHandler(IConfigurationSettingRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<ConfigurationSettingDto>> Handle(
        GetConfigurationSettingByKeyAndScopeQuery request,
        CancellationToken cancellationToken)
    {
        var keyResult = ConfigurationKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<ConfigurationSettingDto>(keyResult.Error);
        }

        var scopeResult = ConfigurationScope.Create(request.ScopeLevel, request.ScopeId);
        if (scopeResult.IsFailure)
        {
            return Result.Failure<ConfigurationSettingDto>(scopeResult.Error);
        }

        var setting = await _repository
            .GetByKeyAndScopeAsync(keyResult.Value, scopeResult.Value, cancellationToken)
            .ConfigureAwait(false);

        return setting is null
            ? Result.Failure<ConfigurationSettingDto>(ConfigurationErrors.SettingNotFound)
            : Result.Success(setting.ToDto());
    }
}
