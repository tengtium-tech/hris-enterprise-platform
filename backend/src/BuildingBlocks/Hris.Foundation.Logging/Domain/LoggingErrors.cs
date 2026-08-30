using Hris.SharedKernel;

namespace Hris.Foundation.Logging.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per
/// docs/02-architecture/04-domain-driven-design/error-pattern.md's "Error Catalog"
/// section.
/// </summary>
public static class LoggingErrors
{
    public static readonly Error ServiceRequired = new(
        "Logging.ServiceRequired",
        "A log entry's originating service name is required.",
        ErrorCategory.Validation);

    public static readonly Error MessageRequired = new(
        "Logging.MessageRequired",
        "A log entry requires a message.",
        ErrorCategory.Validation);
}
