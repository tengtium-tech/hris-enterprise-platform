namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// The ten actions authorization-framework.md's Permission section names: "Create,
/// Read, Update, Delete, Approve, Reject, Export, Import, Execute, Configure." A
/// Simple Enumeration -- fixed, no per-value behavior.
/// </summary>
public enum PermissionAction
{
    Create = 0,
    Read,
    Update,
    Delete,
    Approve,
    Reject,
    Export,
    Import,
    Execute,
    Configure,
}
