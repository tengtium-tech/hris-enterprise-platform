using Hris.Application.Abstractions;
using Hris.Foundation.Numbering.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Numbering.Application.Commands;

/// <summary>
/// The two remaining <see cref="IssuedNumber"/>-level lifecycle transitions -- Release
/// and Archive -- grouped into one file the same way every other Sprint 3/4 framework's
/// own bundled lifecycle commands are.
/// </summary>
public sealed record ReleaseNumberCommand(Guid IssuedNumberId, string Reason) : ICommand<Result>;

internal sealed class ReleaseNumberCommandHandler : IRequestHandler<ReleaseNumberCommand, Result>
{
    private readonly IIssuedNumberRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReleaseNumberCommandHandler(IIssuedNumberRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(ReleaseNumberCommand request, CancellationToken cancellationToken)
    {
        var issuedNumber = await _repository.GetByIdAsync(new IssuedNumberId(request.IssuedNumberId), cancellationToken).ConfigureAwait(false);
        if (issuedNumber is null)
        {
            return Result.Failure(NumberingErrors.IssuedNumberNotFound);
        }

        return issuedNumber.Release(request.Reason, _timeProvider.GetUtcNow());
    }
}

public sealed record ArchiveNumberCommand(Guid IssuedNumberId) : ICommand<Result>;

internal sealed class ArchiveNumberCommandHandler : IRequestHandler<ArchiveNumberCommand, Result>
{
    private readonly IIssuedNumberRepository _repository;

    public ArchiveNumberCommandHandler(IIssuedNumberRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result> Handle(ArchiveNumberCommand request, CancellationToken cancellationToken)
    {
        var issuedNumber = await _repository.GetByIdAsync(new IssuedNumberId(request.IssuedNumberId), cancellationToken).ConfigureAwait(false);
        if (issuedNumber is null)
        {
            return Result.Failure(NumberingErrors.IssuedNumberNotFound);
        }

        return issuedNumber.Archive();
    }
}
