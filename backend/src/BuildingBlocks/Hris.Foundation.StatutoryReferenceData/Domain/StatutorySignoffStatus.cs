namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// Whether a <see cref="StatutoryTableVersion"/> has received the second-reviewer
/// confirmation statutory-reference-data.md's own Update Lifecycle Requirement 2
/// requires before a table is trusted for production computation: "Transcription is
/// verified against the published issuance by a second reviewer before publication."
/// The fixture files this framework is built against
/// (statutory-reference-data/README.md's own Verification Status table) show this is a
/// real, currently-occupied state, not a hypothetical one -- all four of the platform's
/// own shipped tables are <see cref="PendingHumanSignoff"/> as of this framework's own
/// build.
/// </summary>
public enum StatutorySignoffStatus
{
    PendingHumanSignoff = 0,
    SignedOff = 1,
}
