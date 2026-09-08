namespace Hris.Modules.Employment.Domain;

/// <summary>
/// What caused a new <see cref="CompensationRecord"/> to become effective. Carried
/// in the record and in <c>EmploymentCompensationChanged</c>'s payload rather than
/// inferred by consumers. Source:
/// docs/04-modules/employment/domain/domain-events.md's EmploymentCompensationChanged
/// payload ("ChangeSource -- Hire, CompensationChange, Correction").
/// </summary>
public enum CompensationChangeSource
{
    /// <summary>The Employment's initial compensation figure, recorded at hire.</summary>
    Hire = 0,

    /// <summary>An approved Compensation Change from the `compensation` module (EMP-007).</summary>
    CompensationChange = 1,

    /// <summary>A direct correction to a previously recorded figure, with its own approval reference.</summary>
    Correction = 2,
}
