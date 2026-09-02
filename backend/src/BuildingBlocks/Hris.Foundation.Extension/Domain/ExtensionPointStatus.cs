namespace Hris.Foundation.Extension.Domain;

/// <summary>
/// extension-framework.md never states an Extension Point's own lifecycle explicitly
/// -- the document's own Extension Lifecycle diagram (Developed/Validated/Packaged/
/// Installed/Activated/Updated/Deprecated/Removed) belongs to an <em>Extension</em>
/// (a package), not the Point it is registered against, and that diagram's own later
/// stages are `administration`'s TenantExtension (Installed onward) and Phase 8's
/// publishing pipeline (Developed/Validated/Packaged) -- see <see cref="ExtensionPoint"/>'s
/// own remarks. This four-state lifecycle is derived here, grounded directly in two
/// things the document does say: "Only published extension points should be used"
/// (Core Concepts, Extension Point) and "Versioned Extensions" as a named Extension
/// Principle -- a point must be publishable and, eventually, retirable, without ever
/// silently disappearing out from under a module already depending on it.
/// </summary>
public enum ExtensionPointStatus
{
    Draft = 0,
    Published = 1,
    Deprecated = 2,
    Retired = 3,
}
