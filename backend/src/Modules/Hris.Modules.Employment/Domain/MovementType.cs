namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Classifies an Employment Assignment position change for the purpose of choosing
/// which movement Domain Event accompanies the underlying
/// <c>EmploymentAssignmentChanged</c> event. Source:
/// docs/04-modules/employment/domain/employment-assignments.md's own "Promotion and
/// Demotion" section ("A transfer to a Position with a higher Job Grade is treated
/// as a promotion... derive from Job Grade comparison, not from a separate manual
/// flag").
///
/// This platform's own standing "no compile-time cross-module reference" rule means
/// this Aggregate cannot itself query the Position module for Job Grade data to
/// derive this classification -- doing so would require either a compile-time
/// project reference to Hris.Modules.Position (prohibited) or a not-yet-built
/// cross-module read contract this Sprint does not introduce speculatively. The
/// classification is therefore caller-supplied: the three distinct commands
/// (TransferEmploymentCommand, PromoteEmploymentCommand, DemoteEmploymentCommand)
/// each pass a fixed value matching their own name, and their API/UI-layer caller is
/// responsible for having already compared Job Grades (or for the user's own
/// deliberate choice of command) before issuing one. Documented as a deliberate
/// scope decision, not a silently invented mechanism.
/// </summary>
public enum MovementType
{
    /// <summary>A change of Position, organizational unit, or reporting line without a Job Grade change.</summary>
    Lateral = 0,

    /// <summary>A change to a Position of higher Job Grade or scope.</summary>
    Promotion = 1,

    /// <summary>A change to a Position of lower Job Grade or scope.</summary>
    Demotion = 2,
}
