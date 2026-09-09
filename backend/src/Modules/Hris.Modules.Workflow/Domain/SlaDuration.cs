using Hris.SharedKernel;

namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// The time a step or definition allows before it is considered breached. Source:
/// docs/04-modules/workflow/domain/value-objects.md and escalation-and-sla.md.
///
/// Breach is a measurement, not an action (WR-023): reaching it raises a Domain
/// Event for reporting and, where configured, feeds escalation. It never itself
/// approves, rejects, or otherwise resolves the step.
///
/// Named <c>SlaDuration</c> rather than the documents' own <c>SLADuration</c>
/// because .NET's own naming convention (and analyzer CA1709) treats a three-letter
/// initialism as <c>Sla</c>; the concept is unchanged.
/// </summary>
public sealed class SlaDuration : ValueObject
{
    /// <summary>
    /// The platform maximum escalation-and-sla.md requires without naming a value:
    /// "an unbounded SLA is equivalent to no SLA, and a definition author who wants
    /// 'no deadline' should be required to say so explicitly rather than achieve it
    /// by entering an enormous number that looks like an oversight to the next
    /// person who reads the definition." One year is chosen here as the bound: it is
    /// comfortably beyond any legitimate approval deadline while still rejecting the
    /// enormous-number case that document describes. No document in this repository
    /// specifies the figure, so it is a platform constant this module owns rather
    /// than a value silently inferred from one.
    /// </summary>
    public static readonly TimeSpan PlatformMaximum = TimeSpan.FromDays(365);

    public TimeSpan Value { get; }

    private SlaDuration(TimeSpan value)
    {
        Value = value;
    }

    public static Result<SlaDuration> Create(TimeSpan value)
    {
        if (value <= TimeSpan.Zero)
        {
            return Result.Failure<SlaDuration>(WorkflowErrors.SlaDurationMustBePositive);
        }

        return value > PlatformMaximum
            ? Result.Failure<SlaDuration>(WorkflowErrors.SlaDurationExceedsPlatformMaximum)
            : Result.Success(new SlaDuration(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}
