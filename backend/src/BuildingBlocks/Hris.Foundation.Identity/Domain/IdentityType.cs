namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// The seven kinds of identity identity-framework.md's Core Concepts / Identity
/// section names: "Employee, Applicant, Contractor, External User, Administrator,
/// Service Account, Integration Account." A Simple Enumeration per
/// enumeration-pattern.md -- descriptive only, no behavior attaches to the type.
/// </summary>
public enum IdentityType
{
    Employee = 0,
    Applicant,
    Contractor,
    ExternalUser,
    Administrator,
    ServiceAccount,
    IntegrationAccount,
}
