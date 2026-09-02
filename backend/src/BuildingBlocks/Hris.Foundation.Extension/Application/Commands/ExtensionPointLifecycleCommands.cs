using Hris.Application.Abstractions;
using Hris.Foundation.Extension.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Extension.Application.Commands;

/// <summary>
/// The three remaining Extension Point lifecycle transitions -- Publish, Deprecate,
/// Retire -- grouped into one file the same way every other Sprint 3/4 framework's own
/// bundled lifecycle commands are. Each handler is the same shape: look the aggregate
/// up by id, fail with <see cref="ExtensionErrors.ExtensionPointNotFound"/> if it does
/// not exist, otherwise call the one Domain method and return its own
/// <see cref="Result"/>. None needs an explicit save: the aggregate was already loaded
/// through this same <c>DbContext</c>, so the caller's own <c>TransactionBehavior</c>
/// persists the mutation via change tracking alone.
/// </summary>
public sealed record PublishExtensionPointCommand(Guid ExtensionPointId) : ICommand<Result>;

internal sealed class PublishExtensionPointCommandHandler : IRequestHandler<PublishExtensionPointCommand, Result>
{
    private readonly IExtensionPointRepository _repository;
    private readonly TimeProvider _timeProvider;

    public PublishExtensionPointCommandHandler(IExtensionPointRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(PublishExtensionPointCommand request, CancellationToken cancellationToken)
    {
        var extensionPoint = await _repository.GetByIdAsync(new ExtensionPointId(request.ExtensionPointId), cancellationToken).ConfigureAwait(false);
        if (extensionPoint is null)
        {
            return Result.Failure(ExtensionErrors.ExtensionPointNotFound);
        }

        return extensionPoint.Publish(_timeProvider.GetUtcNow());
    }
}

public sealed record DeprecateExtensionPointCommand(Guid ExtensionPointId, string Reason) : ICommand<Result>;

internal sealed class DeprecateExtensionPointCommandHandler : IRequestHandler<DeprecateExtensionPointCommand, Result>
{
    private readonly IExtensionPointRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DeprecateExtensionPointCommandHandler(IExtensionPointRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DeprecateExtensionPointCommand request, CancellationToken cancellationToken)
    {
        var extensionPoint = await _repository.GetByIdAsync(new ExtensionPointId(request.ExtensionPointId), cancellationToken).ConfigureAwait(false);
        if (extensionPoint is null)
        {
            return Result.Failure(ExtensionErrors.ExtensionPointNotFound);
        }

        return extensionPoint.Deprecate(request.Reason, _timeProvider.GetUtcNow());
    }
}

public sealed record RetireExtensionPointCommand(Guid ExtensionPointId) : ICommand<Result>;

internal sealed class RetireExtensionPointCommandHandler : IRequestHandler<RetireExtensionPointCommand, Result>
{
    private readonly IExtensionPointRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RetireExtensionPointCommandHandler(IExtensionPointRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RetireExtensionPointCommand request, CancellationToken cancellationToken)
    {
        var extensionPoint = await _repository.GetByIdAsync(new ExtensionPointId(request.ExtensionPointId), cancellationToken).ConfigureAwait(false);
        if (extensionPoint is null)
        {
            return Result.Failure(ExtensionErrors.ExtensionPointNotFound);
        }

        return extensionPoint.Retire(_timeProvider.GetUtcNow());
    }
}
