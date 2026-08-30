using System.Diagnostics.CodeAnalysis;

namespace Hris.SharedKernel;

/// <summary>
/// Grounded in docs/02-architecture/04-domain-driven-design/error-pattern.md's Error
/// Structure ("Error Code ... Human-readable Description ... Error Category") and
/// Error Characteristics ("Immutable ... Reusable ... Explicit ... Self-describing").
///
/// Each bounded context owns its own error catalog (error-pattern.md, "Error
/// Catalog": "Each bounded context owns its own error catalog") as a static class of
/// <c>readonly</c> <see cref="Error"/> instances -- e.g. a future
/// <c>ConfigurationErrors.AlreadyPublished</c> -- rather than constructing an
/// <see cref="Error"/> inline at the point of failure, so the same violation always
/// carries the same <see cref="Code"/> (`CTR-API-003`).
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "\"Error\" is this platform's own binding ubiquitous-language term "
        + "(error-pattern.md) -- every bounded context's error catalog (EmploymentErrors, "
        + "ConfigurationErrors, ...) is defined in terms of it. Renaming to satisfy "
        + "VB.NET/other-language keyword interop, which this platform does not target, "
        + "would depart from the documented vocabulary for no benefit.")]
public sealed record Error(string Code, string Description, ErrorCategory Category)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorCategory.None);
}
