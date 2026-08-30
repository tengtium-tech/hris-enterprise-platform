namespace Hris.SharedKernel;

/// <summary>
/// Grounded in docs/02-architecture/04-domain-driven-design/error-pattern.md's Error
/// Categories section, which names exactly these five expected-failure categories.
/// <see cref="None"/> is not one of the document's categories -- it exists only as
/// the sentinel <see cref="Error.None"/> carries so a successful <see cref="Result"/>
/// has a well-defined, never-inspected <see cref="Error"/> value.
/// </summary>
public enum ErrorCategory
{
    None = 0,
    Domain,
    Validation,
    Authorization,
    Conflict,
    NotFound,
}
