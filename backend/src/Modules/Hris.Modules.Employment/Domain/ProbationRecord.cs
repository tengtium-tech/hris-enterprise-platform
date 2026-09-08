using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// Represents a probationary period served by an Employment. Source:
/// docs/04-modules/employment/domain/entities.md, ProbationRecord, and
/// probation-confirmation.md's own "Probation Period", "Probation Extension", and
/// "Confirmation" / "Probation Failure Handling" sections. A child Entity of
/// <see cref="Employment"/>, never an Aggregate Root of its own; its constructor and
/// mutating methods are <c>internal</c>, reachable only through
/// <see cref="Employment"/>.
/// </summary>
public sealed class ProbationRecord : Entity<ProbationRecordId>
{
    public DateOnly StartDate { get; private set; }

    public ProbationDuration Duration { get; private set; }

    public DateOnly ExpectedEvaluationDate { get; private set; }

    public ProbationOutcome Outcome { get; private set; }

    public int ExtensionCount { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal ProbationRecord(ProbationRecordId id, DateOnly startDate, ProbationDuration duration, DateTimeOffset createdAtUtc)
        : base(id)
    {
        StartDate = startDate;
        Duration = duration;
        ExpectedEvaluationDate = duration.ComputeEndDate(startDate);
        Outcome = ProbationOutcome.Pending;
        ExtensionCount = 0;
        CreatedAtUtc = createdAtUtc;
    }

    internal Result Extend(ProbationDuration additionalDuration)
    {
        if (Outcome != ProbationOutcome.Pending)
        {
            return Result.Failure(EmploymentErrors.ProbationAlreadyResolved);
        }

        ExpectedEvaluationDate = additionalDuration.ComputeEndDate(ExpectedEvaluationDate);
        ExtensionCount++;
        return Result.Success();
    }

    internal Result Confirm()
    {
        if (Outcome != ProbationOutcome.Pending)
        {
            return Result.Failure(EmploymentErrors.ProbationAlreadyResolved);
        }

        Outcome = ProbationOutcome.Confirmed;
        return Result.Success();
    }

    internal Result Fail()
    {
        if (Outcome != ProbationOutcome.Pending)
        {
            return Result.Failure(EmploymentErrors.ProbationAlreadyResolved);
        }

        Outcome = ProbationOutcome.Failed;
        return Result.Success();
    }
}
