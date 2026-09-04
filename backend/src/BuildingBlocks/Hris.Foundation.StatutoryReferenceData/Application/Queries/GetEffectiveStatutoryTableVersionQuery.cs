using Hris.Application.Abstractions;
using Hris.Foundation.StatutoryReferenceData.Application.Dtos;
using Hris.Foundation.StatutoryReferenceData.Application.Mapping;
using Hris.Foundation.StatutoryReferenceData.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.StatutoryReferenceData.Application.Queries;

/// <summary>
/// The core query this framework exists to answer -- statutory-reference-data.md's own
/// Selection Rule: "Payroll computation selects the table version in force during the
/// payroll period being computed, never the currently active version." Fails explicitly,
/// distinguishing two genuinely different failure reasons rather than collapsing them
/// into one generic "not found," per this document's own Availability Requirement
/// ("Absence of an applicable table for the period being computed is a hard failure, not
/// a warning, and not a fallback to a default") and Relationship to Testing section's own
/// house style of naming the plausible-wrong-implementation failure concretely: a period
/// with no table at all (<see cref="StatutoryReferenceDataErrors.NoApplicableTableForPeriod"/>)
/// is a different operational problem than a period whose table exists but has not yet
/// cleared second-reviewer signoff (<see cref="StatutoryReferenceDataErrors.NoSignedOffApplicableTableForPeriod"/>)
/// -- statutory-reference-data/README.md's own closing line: "A payroll implementation
/// must not compute against a table whose verified.signoff_date is null."
/// </summary>
public sealed record GetEffectiveStatutoryTableVersionQuery(
    string ProgramCode,
    string Country,
    DateTimeOffset PeriodUtc) : IQuery<Result<StatutoryTableVersionDto>>;

internal sealed class GetEffectiveStatutoryTableVersionQueryHandler
    : IRequestHandler<GetEffectiveStatutoryTableVersionQuery, Result<StatutoryTableVersionDto>>
{
    private readonly IStatutoryProgramRepository _programRepository;
    private readonly IStatutoryTableVersionRepository _versionRepository;

    public GetEffectiveStatutoryTableVersionQueryHandler(
        IStatutoryProgramRepository programRepository, IStatutoryTableVersionRepository versionRepository)
    {
        _programRepository = Guard.AgainstNull(programRepository, nameof(programRepository));
        _versionRepository = Guard.AgainstNull(versionRepository, nameof(versionRepository));
    }

    public async Task<Result<StatutoryTableVersionDto>> Handle(
        GetEffectiveStatutoryTableVersionQuery request, CancellationToken cancellationToken)
    {
        var codeResult = StatutoryProgramCode.Create(request.ProgramCode);
        if (codeResult.IsFailure)
        {
            return Result.Failure<StatutoryTableVersionDto>(codeResult.Error);
        }

        var countryResult = StatutoryCountryCode.Create(request.Country);
        if (countryResult.IsFailure)
        {
            return Result.Failure<StatutoryTableVersionDto>(countryResult.Error);
        }

        var program = await _programRepository.GetByCodeAndCountryAsync(
            codeResult.Value, countryResult.Value, cancellationToken).ConfigureAwait(false);
        if (program is null)
        {
            return Result.Failure<StatutoryTableVersionDto>(StatutoryReferenceDataErrors.ProgramNotFound);
        }

        var version = await _versionRepository.GetLatestEffectiveAsOfAsync(
            program.Id, request.PeriodUtc, cancellationToken).ConfigureAwait(false);
        if (version is null)
        {
            return Result.Failure<StatutoryTableVersionDto>(StatutoryReferenceDataErrors.NoApplicableTableForPeriod);
        }

        if (version.Provenance.SignoffStatus != StatutorySignoffStatus.SignedOff)
        {
            return Result.Failure<StatutoryTableVersionDto>(StatutoryReferenceDataErrors.NoSignedOffApplicableTableForPeriod);
        }

        return Result.Success(StatutoryReferenceDataMapper.ToDto(version));
    }
}
