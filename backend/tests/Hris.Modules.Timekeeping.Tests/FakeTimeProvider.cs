namespace Hris.Modules.Timekeeping.Tests;

/// <summary>
/// A fixed clock, per docs/09-testing/unit-and-integration-testing.md §5: "Domain and
/// application code must obtain the current time through an injected abstraction,
/// never through a direct static call."
///
/// That requirement carries unusual weight in this module. Every rule here is
/// effective-dated, and TK-002's whole point is that resolution depends on the date
/// being asked about rather than the date of asking — a test that could not fix the
/// clock could not distinguish the two.
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
