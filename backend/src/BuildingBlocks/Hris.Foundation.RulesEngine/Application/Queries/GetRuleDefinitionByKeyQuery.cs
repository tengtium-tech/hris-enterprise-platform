using Hris.Application.Abstractions;
using Hris.Foundation.RulesEngine.Application.Dtos;
using Hris.Foundation.RulesEngine.Application.Mapping;
using Hris.Foundation.RulesEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.RulesEngine.Application.Queries;

/// <summary>
/// Returns one <see cref="RuleDefinition"/> and every version it has ever had, for
/// administrative/audit display -- the identical shape and purpose
/// <c>GetConfigurationSettingByKeyAndScopeQuery</c> already establishes for
/// Configuration Framework's own sibling lifecycle. Not for runtime rule evaluation,
/// which is <see cref="EvaluateRuleQuery"/>'s job.
/// </summary>
public sealed record GetRuleDefinitionByKeyQuery(string Key) : IQuery<Result<RuleDefinitionDto>>;

internal sealed class GetRuleDefinitionByKeyQueryHandler : IRequestHandler<GetRuleDefinitionByKeyQuery, Result<RuleDefinitionDto>>
{
    private readonly IRuleDefinitionRepository _repository;

    public GetRuleDefinitionByKeyQueryHandler(IRuleDefinitionRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<RuleDefinitionDto>> Handle(GetRuleDefinitionByKeyQuery request, CancellationToken cancellationToken)
    {
        var keyResult = RuleKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<RuleDefinitionDto>(keyResult.Error);
        }

        var definition = await _repository.GetByKeyAsync(keyResult.Value, cancellationToken).ConfigureAwait(false);

        return definition is null
            ? Result.Failure<RuleDefinitionDto>(RuleErrors.RuleDefinitionNotFound)
            : Result.Success(definition.ToDto());
    }
}
