namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Classifies whether a <see cref="LeaveRequest"/> (or a portion of one) is paid. Finalized
/// only at approval (LV-037); a value other than <see cref="Paid"/> is what causes
/// <see cref="LWOPPeriodRecorded"/> at that moment (LV-086). Source:
/// docs/04-modules/leave/domain/value-objects.md.
/// </summary>
public enum PayTreatment
{
    Paid,
    Unpaid,
    PartiallyPaid,
}
