namespace Hris.Foundation.Search.Tests;

/// <summary>
/// A fixed clock for command/query handler tests, per
/// docs/09-testing/unit-and-integration-testing.md 5: "Domain and application code must
/// obtain the current time through an injected abstraction, never through a direct
/// static call." Every handler under test here already takes <see cref="TimeProvider"/>
/// through constructor injection; this is the fixed value tests inject instead of
/// <see cref="TimeProvider.System"/>.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset now)
    {
        _now = now;
    }

    public override DateTimeOffset GetUtcNow() => _now;
}
