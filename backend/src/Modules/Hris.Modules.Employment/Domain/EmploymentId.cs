using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Identity of the <see cref="Employment"/> Aggregate Root. Source:
/// docs/04-modules/employment/domain/entities.md, Employment Aggregate Root Identity
/// ("EmploymentId"). Distinct from the tenant-facing Employment Number
/// (<see cref="EmploymentNumber"/>) -- see employment-numbering.md's own "the two
/// identifiers are not interchangeable" table.
/// </summary>
public readonly record struct EmploymentId(Guid Value) : IStronglyTypedId;
