using FluentValidation;
using Hris.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Hris.Api.Http;

/// <summary>
/// Wired via <c>UseExceptionHandler()</c> in <c>Program.cs</c>, per the .NET 9
/// <see cref="IExceptionHandler"/> extensibility point -- ASP.NET Core's own
/// recommended replacement for a hand-rolled try/catch middleware. Two outcomes:
/// <see cref="ValidationException"/> (thrown by <c>ValidationBehavior</c> before a
/// handler is ever reached) becomes the same 400 Problem Details shape a field-level
/// failure always produces; every other exception is logged and becomes a bare 500,
/// never leaking the exception's own message or stack trace into the response body
/// (`NFR-OB-001`: "Keep error messages free of sensitive data and internal detail").
/// </summary>
internal sealed partial class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var result = exception is ValidationException validationException
            ? ProblemDetailsResults.FromValidationException(validationException, httpContext)
            : UnexpectedFailureResult(httpContext);

        if (exception is not ValidationException)
        {
            LogUnhandledException(_logger, CorrelationIdMiddleware.GetCorrelationId(httpContext), exception);
        }

        await result.ExecuteAsync(httpContext).ConfigureAwait(false);

        return true;
    }

    private static IResult UnexpectedFailureResult(HttpContext httpContext) => Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "An unexpected error occurred.",
        type: "https://docs.hris.example/errors/unexpected",
        instance: httpContext.Request.Path,
        extensions: new Dictionary<string, object?>
        {
            ["code"] = "unexpected",
            ["correlationId"] = CorrelationIdMiddleware.GetCorrelationId(httpContext),
        });

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception. CorrelationId: {CorrelationId}")]
    private static partial void LogUnhandledException(ILogger logger, string correlationId, Exception exception);
}
