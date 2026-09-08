using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// An emergency contact person for an <see cref="Employee"/>. Source:
/// docs/04-modules/employee/domain/entities.md's EmergencyContact section
/// ("Multiple emergency contacts are allowed. One contact may be designated as
/// the primary contact" -- BR-EMP-035/036). Third-party personal data belonging to
/// someone who is not a platform user (employee-contact-information.md's own
/// "Third-Party Data" section): "Collect the minimum necessary -- a name and a
/// contact number, not an address and identification" -- this entity deliberately
/// has no address field, departing from value-objects.md's own looser
/// EmergencyContactInfo field list in favor of that more specific minimization
/// rule. A child Entity of <see cref="Employee"/>, never an Aggregate Root of its
/// own; its constructor and mutators are <c>internal</c> -- at-most-one-primary
/// (BR-EMP-036) is enforced by <see cref="Employee"/>, not by this entity itself,
/// since it requires comparing across sibling contacts.
/// </summary>
public sealed class EmergencyContact : Entity<EmergencyContactId>
{
    public string Name { get; private set; }

    public string Relationship { get; private set; }

    public PhoneNumber Phone { get; private set; }

    public EmailAddress? Email { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    internal EmergencyContact(
        EmergencyContactId id, string name, string relationship, PhoneNumber phone, EmailAddress? email, bool isPrimary,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        Name = name;
        Relationship = relationship;
        Phone = phone;
        Email = email;
        IsPrimary = isPrimary;
        CreatedAtUtc = createdAtUtc;
    }

    internal void UpdateDetails(string name, string relationship, PhoneNumber phone, EmailAddress? email)
    {
        Name = name;
        Relationship = relationship;
        Phone = phone;
        Email = email;
    }

    internal void MarkPrimary() => IsPrimary = true;

    internal void MarkNotPrimary() => IsPrimary = false;
}
