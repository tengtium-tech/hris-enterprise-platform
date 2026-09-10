using Hris.Modules.Timekeeping.Domain;

namespace Hris.Modules.Timekeeping.Application.Commands;

/// <summary>The command-boundary shape of one split-shift period.</summary>
public sealed record ShiftPeriodInput(int Sequence, TimeOnly Start, TimeOnly End)
{
    public ShiftPeriod ToShiftPeriod() => new(Sequence, Start, End);
}
