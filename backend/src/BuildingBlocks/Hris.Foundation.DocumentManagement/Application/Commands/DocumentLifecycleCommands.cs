using Hris.Application.Abstractions;
using Hris.Foundation.DocumentManagement.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.DocumentManagement.Application.Commands;

/// <summary>
/// The five remaining Document Lifecycle transitions -- Review, Approve, Activate,
/// Archive, MarkDisposed -- grouped into one file the same way every other Sprint 3/4
/// framework's own bundled lifecycle commands are (see <c>FileLifecycleCommands</c>).
/// Each handler is the same shape: load the aggregate by id (tenant-checked via
/// <see cref="DocumentLookup"/>), call the one Domain method, and return its own
/// <see cref="Result"/>. None needs an explicit save: the aggregate was already
/// loaded through this same <c>DbContext</c>, so the caller's own
/// <c>TransactionBehavior</c> persists the mutation via change tracking alone.
/// </summary>
public sealed record ReviewDocumentCommand(Guid DocumentId, Guid TenantId) : ICommand<Result>;

internal sealed class ReviewDocumentCommandHandler : IRequestHandler<ReviewDocumentCommand, Result>
{
    private readonly IDocumentRepository _repository;

    public ReviewDocumentCommandHandler(IDocumentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ReviewDocumentCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return documentResult.IsFailure ? Result.Failure(documentResult.Error) : documentResult.Value.Review();
    }
}

public sealed record ApproveDocumentCommand(Guid DocumentId, Guid TenantId) : ICommand<Result>;

internal sealed class ApproveDocumentCommandHandler : IRequestHandler<ApproveDocumentCommand, Result>
{
    private readonly IDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ApproveDocumentCommandHandler(IDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ApproveDocumentCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return documentResult.IsFailure ? Result.Failure(documentResult.Error) : documentResult.Value.Approve(_timeProvider.GetUtcNow());
    }
}

public sealed record ActivateDocumentCommand(Guid DocumentId, Guid TenantId) : ICommand<Result>;

internal sealed class ActivateDocumentCommandHandler : IRequestHandler<ActivateDocumentCommand, Result>
{
    private readonly IDocumentRepository _repository;

    public ActivateDocumentCommandHandler(IDocumentRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ActivateDocumentCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return documentResult.IsFailure ? Result.Failure(documentResult.Error) : documentResult.Value.Activate();
    }
}

public sealed record ArchiveDocumentCommand(Guid DocumentId, Guid TenantId) : ICommand<Result>;

internal sealed class ArchiveDocumentCommandHandler : IRequestHandler<ArchiveDocumentCommand, Result>
{
    private readonly IDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ArchiveDocumentCommandHandler(IDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ArchiveDocumentCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return documentResult.IsFailure ? Result.Failure(documentResult.Error) : documentResult.Value.Archive(_timeProvider.GetUtcNow());
    }
}

public sealed record DisposeDocumentCommand(Guid DocumentId, Guid TenantId) : ICommand<Result>;

internal sealed class DisposeDocumentCommandHandler : IRequestHandler<DisposeDocumentCommand, Result>
{
    private readonly IDocumentRepository _repository;
    private readonly TimeProvider _timeProvider;

    public DisposeDocumentCommandHandler(IDocumentRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(DisposeDocumentCommand request, CancellationToken cancellationToken)
    {
        var documentResult = await DocumentLookup.LoadForTenantAsync(_repository, request.DocumentId, request.TenantId, cancellationToken)
            .ConfigureAwait(false);

        return documentResult.IsFailure ? Result.Failure(documentResult.Error) : documentResult.Value.MarkDisposed(_timeProvider.GetUtcNow());
    }
}
