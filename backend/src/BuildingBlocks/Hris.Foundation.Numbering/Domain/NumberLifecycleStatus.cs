using System.Diagnostics.CodeAnalysis;

namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Source: docs/03-foundation/numbering-framework.md, Number Lifecycle diagram:
/// "Requested -> Reserved -> Generated -> Assigned -> Validated -> Archived."
/// <see cref="Released"/> is the one value not on that linear diagram -- it is the
/// Reservation section's own "Unused reservations should automatically expire"
/// outcome, reachable from <see cref="Requested"/>, <see cref="Reserved"/>, or
/// <see cref="Generated"/> (an abandoned draft, at any point before
/// <see cref="Assigned"/>), and terminal in the same way <see cref="Archived"/> is --
/// per the AI Implementation Guidance's "Never reuse a number after a record is deleted
/// or voided; gaps are acceptable, collisions are not," a released number's own
/// <see cref="IssuedNumber.SequenceValue"/> is never handed to a later request.
/// </summary>
public enum NumberLifecycleStatus
{
    Requested = 0,

    [SuppressMessage(
        "Naming",
        "CA1700:Do not name enum values 'Reserved'",
        Justification = "\"Reserved\" is this document's own domain vocabulary, the " +
            "second step named explicitly in numbering-framework.md's own Number " +
            "Lifecycle diagram -- not a placeholder or future-use marker, which is " +
            "the concern CA1700 actually guards against. Renaming it to satisfy the " +
            "analyzer would break ubiquitous language with the source specification " +
            "for no correctness benefit.")]
    Reserved = 1,

    Generated = 2,
    Assigned = 3,
    Validated = 4,
    Released = 5,
    Archived = 6,
}
