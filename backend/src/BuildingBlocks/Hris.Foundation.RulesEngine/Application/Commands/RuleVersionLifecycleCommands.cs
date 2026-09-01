using Hris.Application.Abstractions;
using Hris.Foundation.Authorization.Application.Queries;
using Hris.Foundation.Authorization.Domain;
using Hris.Foundation.RulesEngine.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.RulesEngine.Application.Commands;

/// <summary>
/// The five remaining Rule Lifecycle transitions (rules-engine.md: "Draft -&gt;
/// Validated -&gt; Published -&gt; Active -&gt; Deprecated -&gt; Archived") not already
/// covered by <see cref="CreateRuleDefinitionCommand"/> (creates the first Draft) and
/// <see cref="CreateNewDraftVersionCommand"/> (drafts the next one).
///
/// Grouped into one file for the identical reason
/// <c>ConfigurationVersionLifecycleCommands</c> states for its own five: each
/// command/handler pair is a mechanical "load the Aggregate by id, call the one
/// <see cref="RuleVersion"/> method that already enforces the transition's invariants,
/// translate the Result" wrapper -- plus, here, the authorization check every
/// rule-management command performs, per rules-engine.md's own Security
/// Considerations: "Only authorized users should publish or modify business rules."
/// See <see cref="CreateRuleDefinitionCommand"/>'s own remarks for why this check does
/// not conflict with this framework's own evaluation-path performance NFR -- these
/// five are infrequent administrative actions, not the high-volume evaluation path.
/// </summary>
internal static class RuleAuthorizationCheck
{
    /// <summary>
    /// The identical authorization check every one of this file's five handlers
    /// performs, factored out once rather than repeated five times -- returns a
    /// failed <see cref="Result"/> (either the query's own failure or
    /// <see cref="RuleErrors.NotAuthorizedToManageRules"/>) when the caller should not
    /// proceed, or <see cref="Result.Success()"/> when it should.
    /// </summary>
    public static async Task<Result> CheckAsync(
        ISender sender, Guid principalId, OrganizationalScopeLevel scopeLevel, Guid scopeId, CancellationToken cancellationToken)
    {
        var authorizationResult = await sender.Send(
            new CheckAuthorizationQuery(principalId, "RuleDefinition", PermissionAction.Configure, scopeLevel, scopeId),
            cancellationToken).ConfigureAwait(false);

        if (authorizationResult.IsFailure)
        {
            return Result.Failure(authorizationResult.Error);
        }

        return authorizationResult.Value.IsAllowed
            ? Result.Success()
            : Result.Failure(RuleErrors.NotAuthorizedToManageRules);
    }
}

public sealed record ValidateRuleVersionCommand(
    Guid RuleDefinitionId, Guid RuleVersionId, Guid RequestingPrincipalId, OrganizationalScopeLevel ScopeLevel, Guid ScopeId)
    : ICommand<Result>;

public sealed record PublishRuleVersionCommand(
    Guid RuleDefinitionId, Guid RuleVersionId, Guid RequestingPrincipalId, OrganizationalScopeLevel ScopeLevel, Guid ScopeId)
    : ICommand<Result>;

public sealed record ActivateRuleVersionCommand(
    Guid RuleDefinitionId, Guid RuleVersionId, Guid RequestingPrincipalId, OrganizationalScopeLevel ScopeLevel, Guid ScopeId)
    : ICommand<Result>;

public sealed record DeprecateRuleVersionCommand(
    Guid RuleDefinitionId, Guid RuleVersionId, Guid RequestingPrincipalId, OrganizationalScopeLevel ScopeLevel, Guid ScopeId)
    : ICommand<Result>;

public sealed record ArchiveRuleVersionCommand(
    Guid RuleDefinitionId, Guid RuleVersionId, Guid RequestingPrincipalId, OrganizationalScopeLevel ScopeLevel, Guid ScopeId)
    : ICommand<Result>;

internal sealed class ValidateRuleVersionCommandHandler : IRequestHandler<ValidateRuleVersionCommand, Result>
{
    private readonly IRuleDefinitionRepository _repository;
    private readonly ISender _sender;

    public ValidateRuleVersionCommandHandler(IRuleDefinitionRepository repository, ISender sender)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
    }

    public async Task<Result> Handle(ValidateRuleVersionCommand request, CancellationToken cancellationToken)
    {
        var authorization = await RuleAuthorizationCheck.CheckAsync(_sender, request.RequestingPrincipalId, request.ScopeLevel, request.ScopeId, cancellationToken)
            .ConfigureAwait(false);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var definition = await _repository
            .GetByIdAsync(new RuleDefinitionId(request.RuleDefinitionId), cancellationToken)
            .ConfigureAwait(false);

        return definition is null
            ? Result.Failure(RuleErrors.RuleDefinitionNotFound)
            : definition.ValidateVersion(new RuleVersionId(request.RuleVersionId));
    }
}

internal sealed class PublishRuleVersionCommandHandler : IRequestHandler<PublishRuleVersionCommand, Result>
{
    private readonly IRuleDefinitionRepository _repository;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public PublishRuleVersionCommandHandler(IRuleDefinitionRepository repository, ISender sender, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishRuleVersionCommand request, CancellationToken cancellationToken)
    {
        var authorization = await RuleAuthorizationCheck.CheckAsync(_sender, request.RequestingPrincipalId, request.ScopeLevel, request.ScopeId, cancellationToken)
            .ConfigureAwait(false);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var definition = await _repository
            .GetByIdAsync(new RuleDefinitionId(request.RuleDefinitionId), cancellationToken)
            .ConfigureAwait(false);

        return definition is null
            ? Result.Failure(RuleErrors.RuleDefinitionNotFound)
            : definition.PublishVersion(new RuleVersionId(request.RuleVersionId), _timeProvider.GetUtcNow());
    }
}

internal sealed class ActivateRuleVersionCommandHandler : IRequestHandler<ActivateRuleVersionCommand, Result>
{
    private readonly IRuleDefinitionRepository _repository;
    private readonly ISender _sender;

    public ActivateRuleVersionCommandHandler(IRuleDefinitionRepository repository, ISender sender)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
    }

    public async Task<Result> Handle(ActivateRuleVersionCommand request, CancellationToken cancellationToken)
    {
        var authorization = await RuleAuthorizationCheck.CheckAsync(_sender, request.RequestingPrincipalId, request.ScopeLevel, request.ScopeId, cancellationToken)
            .ConfigureAwait(false);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var definition = await _repository
            .GetByIdAsync(new RuleDefinitionId(request.RuleDefinitionId), cancellationToken)
            .ConfigureAwait(false);

        return definition is null
            ? Result.Failure(RuleErrors.RuleDefinitionNotFound)
            : definition.ActivateVersion(new RuleVersionId(request.RuleVersionId));
    }
}

internal sealed class DeprecateRuleVersionCommandHandler : IRequestHandler<DeprecateRuleVersionCommand, Result>
{
    private readonly IRuleDefinitionRepository _repository;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public DeprecateRuleVersionCommandHandler(IRuleDefinitionRepository repository, ISender sender, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeprecateRuleVersionCommand request, CancellationToken cancellationToken)
    {
        var authorization = await RuleAuthorizationCheck.CheckAsync(_sender, request.RequestingPrincipalId, request.ScopeLevel, request.ScopeId, cancellationToken)
            .ConfigureAwait(false);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var definition = await _repository
            .GetByIdAsync(new RuleDefinitionId(request.RuleDefinitionId), cancellationToken)
            .ConfigureAwait(false);

        return definition is null
            ? Result.Failure(RuleErrors.RuleDefinitionNotFound)
            : definition.DeprecateVersion(new RuleVersionId(request.RuleVersionId), _timeProvider.GetUtcNow());
    }
}

internal sealed class ArchiveRuleVersionCommandHandler : IRequestHandler<ArchiveRuleVersionCommand, Result>
{
    private readonly IRuleDefinitionRepository _repository;
    private readonly ISender _sender;
    private readonly TimeProvider _timeProvider;

    public ArchiveRuleVersionCommandHandler(IRuleDefinitionRepository repository, ISender sender, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _sender = Guard.AgainstNull(sender, nameof(sender));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveRuleVersionCommand request, CancellationToken cancellationToken)
    {
        var authorization = await RuleAuthorizationCheck.CheckAsync(_sender, request.RequestingPrincipalId, request.ScopeLevel, request.ScopeId, cancellationToken)
            .ConfigureAwait(false);
        if (authorization.IsFailure)
        {
            return authorization;
        }

        var definition = await _repository
            .GetByIdAsync(new RuleDefinitionId(request.RuleDefinitionId), cancellationToken)
            .ConfigureAwait(false);

        return definition is null
            ? Result.Failure(RuleErrors.RuleDefinitionNotFound)
            : definition.ArchiveVersion(new RuleVersionId(request.RuleVersionId), _timeProvider.GetUtcNow());
    }
}
