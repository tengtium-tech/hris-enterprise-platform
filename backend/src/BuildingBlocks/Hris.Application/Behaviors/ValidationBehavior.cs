using FluentValidation;
using MediatR;

namespace Hris.Application.Behaviors;

/// <summary>
/// application-pipeline.md's Validation Behavior: "Validation is executed using
/// FluentValidation... Invalid requests never reach the handler." Runs for every
/// <c>IRequest</c> -- Command or Query alike, unlike <see cref="TransactionBehavior{TRequest,TResponse}"/>
/// -- because a malformed query (e.g. an empty required filter) deserves the same
/// "rejected before the handler" treatment coding-standards.md's Application Layer
/// convention describes for commands: "Validation... run before the handler executes,
/// not interleaved with handler logic."
///
/// Registered as an open generic; a request with no registered
/// <see cref="IValidator{T}"/> passes through untouched, so a framework can add
/// validators incrementally without this behavior itself changing.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var failures = (await Task.WhenAll(
                    _validators.Select(validator => validator.ValidateAsync(context, cancellationToken)))
                .ConfigureAwait(false))
                .SelectMany(result => result.Errors)
                .Where(failure => failure is not null)
                .ToList();

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next().ConfigureAwait(false);
    }
}
