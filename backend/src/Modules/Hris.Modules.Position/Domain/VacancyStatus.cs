namespace Hris.Modules.Position.Domain;

/// <summary>
/// Staffing status of a <see cref="Position"/>, independent of <see cref="PositionStatus"/>
/// (position-lifecycle.md's own Vacancy Relationship table: "Lifecycle governs
/// operational availability, while Vacancy Status reflects staffing"). Source:
/// docs/04-modules/position/domain/value-objects.md's Vacancy Status section, which
/// names four illustrative values (Vacant, Filled, Reserved, Frozen), narrowed here
/// to the two this module's own application/commands.md and
/// application/command-handlers.md actually name a command for ("Mark Position
/// Vacant", "Mark Position Filled") -- the identical "trust the concrete
/// command/rule list over the looser narrative value list" resolution the
/// Organization module's own OrganizationalUnitStatus applied to its four-state
/// narrative lifecycle diagram.
/// </summary>
public enum VacancyStatus
{
    Vacant = 0,
    Filled = 1,
}
