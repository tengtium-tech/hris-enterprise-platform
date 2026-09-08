using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="EmploymentAssignment"/> Aggregate Root. Source:
/// docs/04-modules/employment/domain/entities.md, Employment Assignment Aggregate
/// Root Identity ("EmploymentAssignmentId"). ADR-0008's own cross-module ownership
/// diagram names this third layer "Assignment" with identity "AssignmentId" as
/// architectural shorthand; this module's own three domain documents
/// (aggregates.md, entities.md, value-objects.md) are internally three-way
/// consistent on the fuller "EmploymentAssignment"/"EmploymentAssignmentId" form,
/// which also matches the sibling Aggregates' own "Employment"-prefixed naming
/// (<see cref="EmploymentContract"/>) -- the concrete type name used throughout this
/// module's own code. ADR-0008's substantive ownership decision (Position,
/// organizational unit, and reporting line form a layer separate from Employment and
/// EmploymentContract) is unaffected either way; only the shorthand label differs.
/// </summary>
public readonly record struct EmploymentAssignmentId(Guid Value) : IStronglyTypedId;
