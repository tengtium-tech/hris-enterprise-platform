namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// How a <see cref="StatutoryTableVersion"/>'s own numbers were obtained, per
/// statutory-reference-data/README.md's own Verification Status table: "whether it was
/// transcribed from the issuing agency's own primary document versus a secondary
/// summary."
/// </summary>
public enum StatutoryVerificationSourceType
{
    PrimarySourceRead = 0,
    SecondarySummary = 1,
}
