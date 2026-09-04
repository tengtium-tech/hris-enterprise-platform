namespace Hris.Foundation.StatutoryReferenceData.Application.Dtos;

/// <summary>
/// The read-side shape <c>GetStatutoryProgramQuery</c>/<c>ListStatutoryProgramsQuery</c>
/// return, per dto-design.md's own convention.
/// </summary>
public sealed record StatutoryProgramDto(
    Guid StatutoryProgramId,
    string Code,
    string Country,
    string DisplayName,
    DateTimeOffset RegisteredAtUtc);
