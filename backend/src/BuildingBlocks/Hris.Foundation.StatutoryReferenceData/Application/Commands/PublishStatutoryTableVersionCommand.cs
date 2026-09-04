using Hris.Application.Abstractions;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.StatutoryReferenceData.Application.Commands;

/// <summary>
/// Publishes a new table version for an already-registered program. Every new version
/// starts <see cref="StatutorySignoffStatus.PendingHumanSignoff"/> -- this command
/// carries no signoff fields of its own, matching every one of the platform's own four
/// shipped fixture files (statutory-reference-data/README.md: "primary-source-read for
/// the source, and pending-human-signoff for the review step"); <c>RecordStatutoryTableVersionSignoffCommand</c>
/// is the only path to <see cref="StatutorySignoffStatus.SignedOff"/>.
/// </summary>
public sealed record PublishStatutoryTableVersionCommand(
    Guid StatutoryProgramId,
    string VersionLabel,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    string IssuingAuthority,
    string IssuanceReference,
    DateTimeOffset PublicationDateUtc,
    StatutoryVerificationSourceType SourceType,
    DateTimeOffset ReadDateUtc,
    string ScheduleData) : ICommand<Result<Guid>>;

internal sealed class PublishStatutoryTableVersionCommandHandler
    : IRequestHandler<PublishStatutoryTableVersionCommand, Result<Guid>>
{
    private readonly IStatutoryProgramRepository _programRepository;
    private readonly IStatutoryTableVersionRepository _versionRepository;
    private readonly TimeProvider _timeProvider;

    public PublishStatutoryTableVersionCommandHandler(
        IStatutoryProgramRepository programRepository,
        IStatutoryTableVersionRepository versionRepository,
        TimeProvider timeProvider)
    {
        _programRepository = Guard.AgainstNull(programRepository, nameof(programRepository));
        _versionRepository = Guard.AgainstNull(versionRepository, nameof(versionRepository));
        _timeProvider = Guard.AgainstNull(timeProvider, nameof(timeProvider));
    }

    public async Task<Result<Guid>> Handle(PublishStatutoryTableVersionCommand request, CancellationToken cancellationToken)
    {
        var programId = new StatutoryProgramId(request.StatutoryProgramId);

        var program = await _programRepository.GetByIdAsync(programId, cancellationToken).ConfigureAwait(false);
        if (program is null)
        {
            return Result.Failure<Guid>(StatutoryReferenceDataErrors.ProgramNotFound);
        }

        var versionLabelResult = StatutoryTableVersionLabel.Create(request.VersionLabel);
        if (versionLabelResult.IsFailure)
        {
            return Result.Failure<Guid>(versionLabelResult.Error);
        }

        if (await _versionRepository.ExistsByProgramAndVersionLabelAsync(programId, versionLabelResult.Value, cancellationToken)
            .ConfigureAwait(false))
        {
            return Result.Failure<Guid>(StatutoryReferenceDataErrors.DuplicateVersionLabel);
        }

        var provenance = new StatutoryTableProvenance(
            request.IssuingAuthority,
            request.IssuanceReference,
            request.PublicationDateUtc,
            request.SourceType,
            request.ReadDateUtc,
            StatutorySignoffStatus.PendingHumanSignoff,
            null,
            null);

        var publishResult = StatutoryTableVersion.Publish(
            programId,
            versionLabelResult.Value,
            request.EffectiveFromUtc,
            request.EffectiveToUtc,
            provenance,
            request.ScheduleData,
            _timeProvider.GetUtcNow());
        if (publishResult.IsFailure)
        {
            return Result.Failure<Guid>(publishResult.Error);
        }

        await _versionRepository.AddAsync(publishResult.Value, cancellationToken).ConfigureAwait(false);

        return Result.Success(publishResult.Value.Id.Value);
    }
}
