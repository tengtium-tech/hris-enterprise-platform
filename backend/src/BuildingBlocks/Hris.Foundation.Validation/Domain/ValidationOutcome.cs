using Hris.SharedKernel;

namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// Every <see cref="ValidationFailure"/> found in one validation pass, per
/// validation-framework.md's own Implementation Guidance: "Return all validation
/// failures together where practical, rather than failing on the first, so bulk and
/// import operations can report per-record errors." Deliberately a different shape
/// from <see cref="Result"/> (single <see cref="Error"/>) rather than reusing it:
/// validation genuinely needs to report several simultaneous problems, which a type
/// designed around exactly one <see cref="SharedKernel.Error"/> cannot represent
/// without discarding all but the first.
///
/// <see cref="IsValid"/>'s exact severity-to-blocking mapping per
/// <see cref="ValidationPolicy"/> is this class's own interpretation of that
/// section's brief description, not a distinction the source document spells out at
/// this level of detail -- stated explicitly here rather than left implicit:
/// <c>RejectOnError</c> blocks on Error or Critical; <c>ContinueWithWarning</c> blocks
/// only on Critical (Error is downgraded to non-blocking but still reported);
/// <c>WarningOnly</c> and <c>ValidationDisabled</c> never block.
/// </summary>
public sealed class ValidationOutcome
{
    private readonly IReadOnlyList<ValidationFailure> _failures;

    public ValidationPolicy Policy { get; }

    public IReadOnlyList<ValidationFailure> Failures => _failures;

    public ValidationOutcome(IReadOnlyList<ValidationFailure> failures, ValidationPolicy policy)
    {
        Guard.AgainstNull(failures, nameof(failures));
        _failures = failures;
        Policy = policy;
    }

    public static ValidationOutcome Clean(ValidationPolicy policy) => new([], policy);

    public bool IsValid => Policy switch
    {
        ValidationPolicy.WarningOnly => true,
        ValidationPolicy.ValidationDisabled => true,
        ValidationPolicy.ContinueWithWarning => !_failures.Any(f => f.Severity == ValidationSeverity.Critical),
        _ => !_failures.Any(f => f.Severity is ValidationSeverity.Error or ValidationSeverity.Critical),
    };
}
