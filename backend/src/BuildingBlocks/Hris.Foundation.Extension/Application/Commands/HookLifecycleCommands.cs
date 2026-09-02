using Hris.Application.Abstractions;
using Hris.Foundation.Extension.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Extension.Application.Commands;

/// <summary>
/// The three remaining Hook lifecycle transitions -- Disable, Enable, Remove --
/// grouped into one file, the same shape <c>ExtensionPointLifecycleCommands.cs</c>
/// establishes for its own sibling aggregate.
/// </summary>
public sealed record DisableHookCommand(Guid HookId) : ICommand<Result>;

internal sealed class DisableHookCommandHandler : IRequestHandler<DisableHookCommand, Result>
{
    private readonly IHookRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DisableHookCommandHandler(IHookRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DisableHookCommand request, CancellationToken cancellationToken)
    {
        var hook = await _repository.GetByIdAsync(new HookId(request.HookId), cancellationToken).ConfigureAwait(false);
        if (hook is null)
        {
            return Result.Failure(ExtensionErrors.HookNotFound);
        }

        return hook.Disable(_timeProvider.GetUtcNow());
    }
}

public sealed record EnableHookCommand(Guid HookId) : ICommand<Result>;

internal sealed class EnableHookCommandHandler : IRequestHandler<EnableHookCommand, Result>
{
    private readonly IHookRepository _repository;
    private readonly TimeProvider _timeProvider;

    public EnableHookCommandHandler(IHookRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(EnableHookCommand request, CancellationToken cancellationToken)
    {
        var hook = await _repository.GetByIdAsync(new HookId(request.HookId), cancellationToken).ConfigureAwait(false);
        if (hook is null)
        {
            return Result.Failure(ExtensionErrors.HookNotFound);
        }

        return hook.Enable(_timeProvider.GetUtcNow());
    }
}

public sealed record RemoveHookCommand(Guid HookId) : ICommand<Result>;

internal sealed class RemoveHookCommandHandler : IRequestHandler<RemoveHookCommand, Result>
{
    private readonly IHookRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RemoveHookCommandHandler(IHookRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RemoveHookCommand request, CancellationToken cancellationToken)
    {
        var hook = await _repository.GetByIdAsync(new HookId(request.HookId), cancellationToken).ConfigureAwait(false);
        if (hook is null)
        {
            return Result.Failure(ExtensionErrors.HookNotFound);
        }

        return hook.Remove(_timeProvider.GetUtcNow());
    }
}
