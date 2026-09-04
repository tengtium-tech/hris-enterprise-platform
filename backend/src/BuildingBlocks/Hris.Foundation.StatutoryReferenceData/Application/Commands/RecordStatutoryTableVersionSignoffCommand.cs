using Hris.Application.Abstractions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.StatutoryReferenceData.Application.Commands;

/// <summary>
/// Records the second-reviewer signoff Update Lifecycle Requirement 2 requires. No
/// <c>UpdateAsync</c> call: a version loaded through <see cref="IStatutoryTableVersionRepository.GetByIdAsync"/>
/// is already tracked by the same <c>HrisDbContext</c>, so the caller's own
/// <c>TransactionBehavior</c> persists the mutation via change tracking alone -- the
/// identical pattern every other Sprint 3/4 framework's own lifecycle command handler
/// already establishes.
/// </summary>
public sealed record RecordStatutoryTableVersionSignoffCommand(
    Guid StatutoryTableVersionId,
    string SignoffBy) : ICommand<Result>;

internal sealed class RecordStatutoryTableVersionSignoffCommandHandler
    : IRequestHandler<RecordStatutoryTableVersionSignoffCommand, Result>
{
    private readonly IStatutoryTableVersionRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecordStatutoryTableVersionSignoffCommandHandler(
        IStatutoryTableVersionRepository repository, TimeProvider timeProvider)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result> Handle(RecordStatutoryTableVersionSignoffCommand request, CancellationToken cancellationToken)
    {
        var version = await _repository.GetByIdAsync(
            new StatutoryTableVersionId(request.StatutoryTableVersionId), cancellationToken).ConfigureAwait(false);
        if (version is null)
        {
            return Result.Failure(StatutoryReferenceDataErrors.StatutoryTableVersionNotFound);
        }

        return version.RecordSignoff(_timeProvider.GetUtcNow(), request.SignoffBy);
    }
}
