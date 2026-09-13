using Hris.SharedKernel;

namespace Hris.Modules.Attendance.Domain;

/// <summary>
/// A pointer to an encrypted, non-reversible biometric template held in a dedicated
/// secure store, per docs/04-modules/attendance/domain/value-objects.md. The
/// <see cref="BiometricEnrollment"/> aggregate holds this reference, never the
/// template's bytes or a raw biometric image (AT-052).
/// </summary>
public readonly record struct BiometricTemplateReference
{
    // Named Value, not the more literal "Pointer" the domain language above uses --
    // CA1720 flags an identifier that reads as a native-pointer type name, and Value
    // is this codebase's own established name for a validated single-string value
    // object's inner value (see e.g. ShiftCode.Value).
    public string Value { get; }

    private BiometricTemplateReference(string value) => Value = value;

    public static Result<BiometricTemplateReference> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<BiometricTemplateReference>(AttendanceErrors.BiometricTemplateReferenceRequired);
        }

        return Result.Success(new BiometricTemplateReference(value.Trim()));
    }
}
