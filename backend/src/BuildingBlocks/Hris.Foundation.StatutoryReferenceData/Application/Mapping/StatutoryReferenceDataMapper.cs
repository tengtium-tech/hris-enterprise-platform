using Hris.Foundation.StatutoryReferenceData.Application.Dtos;
using Hris.Foundation.StatutoryReferenceData.Domain;

namespace Hris.Foundation.StatutoryReferenceData.Application.Mapping;

/// <summary>
/// Domain-to-DTO mapping, kept as a plain static class rather than a library such as
/// AutoMapper, per mapping.md's own stated preference for explicit mapping code -- the
/// identical choice every other Sprint 3/4 framework's own mapper already establishes.
/// </summary>
internal static class StatutoryReferenceDataMapper
{
    public static StatutoryProgramDto ToDto(StatutoryProgram program) => new(
        program.Id.Value,
        program.Code.Value,
        program.Country.Value,
        program.DisplayName,
        program.RegisteredAtUtc);

    public static StatutoryTableVersionDto ToDto(StatutoryTableVersion version) => new(
        version.Id.Value,
        version.StatutoryProgramId.Value,
        version.VersionLabel.Value,
        version.EffectiveFromUtc,
        version.EffectiveToUtc,
        version.Provenance.IssuingAuthority,
        version.Provenance.IssuanceReference,
        version.Provenance.PublicationDateUtc,
        version.Provenance.SourceType.ToString(),
        version.Provenance.ReadDateUtc,
        version.Provenance.SignoffStatus.ToString(),
        version.Provenance.SignoffDateUtc,
        version.Provenance.SignoffBy,
        version.ScheduleData,
        version.PublishedAtUtc);
}
