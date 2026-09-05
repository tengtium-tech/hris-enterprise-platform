using FluentValidation;
using Hris.Api.Middleware;
using Hris.SharedKernel;

namespace Hris.Api.Http;

/// <summary>
/// api-standards.md's own Error Response Format section, built once here so every
/// future endpoint produces the identical RFC 7807 shape rather than a bespoke error
/// body per module. Two distinct sources converge on the same shape:
/// <see cref="FromError"/> for a business failure a handler already returned as a
/// failed <c>Result</c>, and <see cref="FromValidationException"/> for a
/// <see cref="ValidationException"/> <c>ValidationBehavior</c> throws before a
/// handler is ever reached ("Invalid requests never reach the handler" --
/// application-pipeline.md) -- the only path that ever populates the `errors` array,
/// since a <c>Result</c>-carried <see cref="Error"/> is never itself field-level.
/// </summary>
internal static class ProblemDetailsResults
{
    private const string _validationCode = "validation.failed";

    public static IResult FromError(Error error, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(httpContext);

        var status = error.Category.ToHttpStatus();

        return Results.Problem(
            statusCode: status,
            title: error.Description,
            type: $"https://docs.hris.example/errors/{error.Code}",
            instance: httpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["correlationId"] = CorrelationIdMiddleware.GetCorrelationId(httpContext),
            });
    }

    public static IResult FromValidationException(ValidationException exception, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(httpContext);

        var errors = exception.Errors
            .Select(failure => new Dictionary<string, object?>
            {
                ["field"] = failure.PropertyName,
                ["issue"] = failure.ErrorCode,
            })
            .ToList();

        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "One or more fields failed validation.",
            type: $"https://docs.hris.example/errors/{_validationCode}",
            instance: httpContext.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = _validationCode,
                ["correlationId"] = CorrelationIdMiddleware.GetCorrelationId(httpContext),
                ["errors"] = errors,
            });
    }
}

internal static class ResultHttpResultExtensions
{
    public static IResult ToHttpResult(this Result result, HttpContext httpContext, Func<IResult>? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? onSuccess?.Invoke() ?? Results.NoContent()
            : ProblemDetailsResults.FromError(result.Error, httpContext);
    }

    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext, Func<T, IResult>? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess
            ? onSuccess is not null ? onSuccess(result.Value) : Results.Ok(result.Value)
            : ProblemDetailsResults.FromError(result.Error, httpContext);
    }
}
