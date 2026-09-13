namespace Hris.Modules.Leave.Tests;

/// <summary>
/// Shared fixtures, each building the minimum valid shape a test needs so a failure
/// points at the rule under test rather than at incidental setup.
/// </summary>
internal static class TestLeave
{
    public static readonly DateTimeOffset NowUtc = new(2026, 9, 13, 9, 0, 0, TimeSpan.Zero);

    public static DateOnly Today => DateOnly.FromDateTime(NowUtc.UtcDateTime);
}
