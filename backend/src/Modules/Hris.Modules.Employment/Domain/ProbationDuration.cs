using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The configured length of a probationary period, in days. Source:
/// docs/04-modules/employment/domain/value-objects.md, ProbationDuration. Statutory
/// bounds (Philippine practice: generally a maximum of six months) are tenant policy
/// defaults per probation-confirmation.md and employment-policies.md's own
/// Policy-vs-Invariant distinction, not enforced here as a hard-coded Domain
/// invariant -- this Value Object validates only that the duration is positive; the
/// Rules Engine integration that would enforce a configurable statutory ceiling is
/// out of scope for this Sprint (see employment-policies.md's own deferred Rules
/// Engine integration note in this module's PR description).
/// </summary>
public sealed class ProbationDuration : ValueObject
{
    public int Days { get; }

    private ProbationDuration(int days)
    {
        Days = days;
    }

    public static Result<ProbationDuration> Create(int days)
    {
        return days <= 0
            ? Result.Failure<ProbationDuration>(EmploymentErrors.ProbationDurationMustBePositive)
            : Result.Success(new ProbationDuration(days));
    }

    public DateOnly ComputeEndDate(DateOnly startDate) => startDate.AddDays(Days);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Days;
    }

    public override string ToString() => $"{Days} days";
}
