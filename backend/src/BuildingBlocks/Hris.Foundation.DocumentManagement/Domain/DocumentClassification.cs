namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Source: docs/03-foundation/document-management.md, Classification ("Documents may
/// be classified as... Classification determines handling requirements"). A closed,
/// platform-wide security taxonomy -- unlike <see cref="Document.Category"/>, which is
/// a validated string precisely because business modules invent their own document
/// categories in Phase 2+, this document's own Classification section names exactly
/// five fixed values with no stated extension point, so a closed enum is the correct
/// structural choice here.
/// </summary>
public enum DocumentClassification
{
    Public = 0,
    Internal,
    Confidential,
    HighlyConfidential,
    Restricted,
}
