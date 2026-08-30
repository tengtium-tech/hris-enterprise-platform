namespace Hris.SharedKernel;

/// <summary>
/// Grounded in logging-framework.md's Correlation ID section: "Every request should
/// include a Correlation ID to trace activity across multiple services ... should
/// propagate across APIs, background jobs, and events." Originally built inside
/// Logging Framework; moved here once Event Framework needed the identical concept
/// (its own Event Structure section separately names "Correlation Identifier"),
/// confirming genuine cross-Bounded-Context use rather than the speculative growth
/// shared-kernel.md's own "grow slowly and intentionally" principle warns against.
///
/// Backed by <see cref="Guid"/> rather than the logging document's own illustrative
/// <c>REQ-9C6A5B42-8D4F</c> string format -- that example is a *display* rendering,
/// not a storage contract, and a Guid is exactly what ASP.NET Core's own
/// <c>HttpContext.TraceIdentifier</c>/<c>Activity.Id</c> infrastructure already
/// generates per request, which this type will wrap at the Infrastructure boundary.
/// </summary>
public sealed class CorrelationId : ValueObject
{
    public Guid Value { get; }

    private CorrelationId(Guid value)
    {
        Value = value;
    }

    public static Result<CorrelationId> Create(Guid value)
    {
        return value == Guid.Empty
            ? Result.Failure<CorrelationId>(SharedKernelErrors.CorrelationIdRequired)
            : Result.Success(new CorrelationId(value));
    }

    public static CorrelationId NewId() => new(Guid.NewGuid());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"REQ-{Value:N}"[..17].ToUpperInvariant();
}
