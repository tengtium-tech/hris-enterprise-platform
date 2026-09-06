namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Source: docs/03-foundation/document-management.md, Version ("Each version should
/// maintain... Status") and Version Management ("Previous versions should never be
/// overwritten"). Exactly two values are needed to satisfy that requirement: the one
/// version currently in force, and every prior version, kept exactly as it was and
/// never deleted when superseded.
/// </summary>
public enum DocumentVersionStatus
{
    Active = 0,
    Superseded,
}
