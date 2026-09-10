using Hris.Modules.Timekeeping.Domain;

namespace Hris.Modules.Timekeeping.Application.Commands;

/// <summary>
/// The command-boundary shape of a break, converted to the domain's own
/// <see cref="BreakRule"/> by <see cref="ToBreakRule"/>. Flat and primitive-typed,
/// because a command is a request object crossing the application boundary and
/// accepting a constructed domain value there would let a caller bypass the
/// validation the domain type performs.
/// </summary>
public sealed record BreakRuleInput(
    BreakKind Kind, TimeSpan Duration, TimeOnly? WindowStart, TimeOnly? WindowEnd, bool Paid, bool Mandatory)
{
    public BreakRule ToBreakRule() => new(
        Kind,
        Duration,
        WindowStart is not null && WindowEnd is not null ? new TimeWindowSnapshot(WindowStart.Value, WindowEnd.Value) : null,
        Paid,
        Mandatory);
}
