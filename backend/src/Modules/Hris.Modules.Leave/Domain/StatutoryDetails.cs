namespace Hris.Modules.Leave.Domain;

/// <summary>
/// Attached to a <see cref="LeaveRequest"/> when its leave type is statutory and carries
/// conditions beyond a plain date range (LV-034). One value object with a
/// <see cref="Variant"/> discriminator rather than five separate <c>LeaveRequest</c>
/// subtypes — every variant still moves through the identical request lifecycle, so
/// duplicating that lifecycle five times to vary only eligibility conditions and
/// documentation shape would be exactly what this discriminated shape avoids. Source:
/// docs/04-modules/leave/domain/value-objects.md.
///
/// Field groups are named per variant rather than a fully polymorphic type hierarchy, the
/// same flattened-record shape this platform already uses for other bundled configuration
/// (compare <c>LeavePolicyRuleset</c>). <see cref="SupportingDocumentReference"/> for VAWC
/// and Special Leave Benefit is held under elevated confidentiality regardless of the
/// viewer's ordinary <c>ReportingLine</c> scope (LV-083, LV-084,
/// ../security/field-security.md) — enforced at the query and field-security layer, never
/// by this value object itself.
/// </summary>
public sealed record StatutoryDetails(
    StatutoryDetailsVariant Variant,
    // Maternity (RA 11210).
    DateOnly? DeliveryDate,
    bool? IsMiscarriageOrEmergencyTermination,
    int? DaysAllocatedToFather,
    bool? UnpaidExtensionElected,
    // Paternity (RA 8187).
    bool? IsCohabitingWithSpouseAtDelivery,
    bool? IsWithinFirstFourDeliveriesOfMarriage,
    // Solo Parent (RA 8972, as amended by RA 11861).
    string? SoloParentIdReference,
    DateOnly? SoloParentIdExpiryDate,
    // VAWC (RA 9262) and Special Leave Benefit (RA 9710) — elevated confidentiality.
    string? SupportingDocumentReference);
