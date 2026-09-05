using Hris.SharedKernel;

namespace Hris.Api.Http;

/// <summary>
/// api-standards.md's own HTTP Status Code Table, the platform-wide mapping from a
/// <see cref="Hris.SharedKernel.Error"/>'s own <see cref="ErrorCategory"/> to the HTTP
/// status a failed <c>Result</c> becomes at this, the Presentation layer boundary --
/// never inside a Domain or Application layer type, which must stay ignorant of HTTP
/// entirely (coding-standards.md's own Domain Layer convention, extended one layer
/// up the same way <c>Hris.Application</c>'s own csproj header already extends
/// <c>CTR-ARC-001</c>'s reasoning for itself).
///
/// <see cref="ErrorCategory.Entitlement"/> and <see cref="ErrorCategory.Authorization"/>
/// both map to 403 -- api-standards.md's own "A distinct, documented code for
/// entitlement failure, carried at 403" section states plainly that the two are
/// deliberately the same status code, distinguished only by the response body's own
/// `code` field (<see cref="Error.Code"/>), never by the status code itself
/// (`CTR-ENT-007`).
/// </summary>
internal static class ErrorCategoryHttpStatus
{
    public static int ToHttpStatus(this ErrorCategory category) => category switch
    {
        ErrorCategory.Validation => StatusCodes.Status400BadRequest,
        ErrorCategory.Entitlement => StatusCodes.Status403Forbidden,
        ErrorCategory.Authorization => StatusCodes.Status403Forbidden,
        ErrorCategory.NotFound => StatusCodes.Status404NotFound,
        ErrorCategory.Conflict => StatusCodes.Status409Conflict,
        ErrorCategory.Domain => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError,
    };
}
