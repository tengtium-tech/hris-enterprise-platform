using System.Diagnostics.CodeAnalysis;
using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Aggregate Root representing the person-level master record for an individual
/// employed (or formerly employed) by the tenant. Source:
/// docs/04-modules/employee/domain/aggregates.md and entities.md, both of which
/// name this the module's own primary Aggregate Root; ADR-0008's own canonical
/// workforce domain model.
///
/// Per ADR-0008 and this module's own extensively self-corrected docs (aggregates.md,
/// entities.md, employee-profile.md, employee-status.md all carry an "earlier
/// version of this document..." note reversing an original design that gave
/// Employee its own Employment Type, Organizational Assignment, and Position),
/// this Aggregate owns only what describes the *person*: identity (<see cref="Number"/>),
/// personal information, contact information, statutory (government) information,
/// banking information, emergency contacts, family members, and
/// <see cref="LifecycleStage"/>. It owns none of Employment Type, Employment
/// Lifecycle, Operational Status, organizational placement, or position -- those
/// are owned by the Employment module's own <c>Employment</c> and
/// <c>EmploymentAssignment</c> Aggregate Roots, which this Aggregate never
/// references even by <see cref="Guid"/> (the relationship runs the other
/// direction: Employment references Employee by <see cref="Guid"/>, per
/// Employment.cs's own <c>EmployeeId</c> property).
///
/// <see cref="EmployeeLifecycleStage"/> is the sole status axis this Aggregate
/// owns; BR-EMP-018 lists nine values, of which this Aggregate can only ever
/// reach seven starting from <see cref="Create"/> (Candidate and Pre-Hire predate
/// Employee's own existence -- see <see cref="EmployeeLifecycleStage"/>'s own
/// remarks). Leave, Suspension, Resignation, and Termination are Employment-owned
/// facts (Operational Status values and separation reasons) that never change
/// this Aggregate's own Lifecycle Stage -- an Employee remains <see cref="EmployeeLifecycleStage.Active"/>
/// throughout all four, per employee-status.md's own "Promotion, Transfer, and
/// Demotion Are Not Status Changes" reasoning extended to these.
///
/// "Employee" as both the module's own root namespace segment and this class's
/// own name reproduces the exact CS0118 name-resolution collision found while
/// building Organization, Position, and Employment (see feedback memory
/// <c>feedback-module-namespace-collision</c>): any reference to this type from
/// outside its own Domain namespace must be fully qualified as
/// <c>Hris.Modules.Employee.Domain.Employee</c>, never the short
/// <c>Domain.Employee</c> partial form -- including inside the test project.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1724:Type names should not match namespaces",
    Justification = "\"Employee\" is this module's own binding ubiquitous-language term "
        + "(docs/04-modules/employee/) -- the identical precedent Hris.Modules.Organization.Domain.Organization, "
        + "Hris.Modules.Position.Domain.Position, and Hris.Modules.Employment.Domain.Employment already set. Renaming "
        + "the Aggregate Root to avoid a namespace collision would depart from the documented business vocabulary for "
        + "no benefit.")]
public sealed class Employee : AggregateRoot<EmployeeId>
{
    private readonly List<EmergencyContact> _emergencyContacts = [];
    private readonly List<FamilyMember> _familyMembers = [];

    public Guid TenantId { get; }

    public EmployeeNumber Number { get; }

    public PersonName Name { get; private set; } = null!;

    public DateOnly DateOfBirth { get; private set; }

    public string? BirthPlace { get; private set; }

    public Gender Gender { get; private set; }

    public CivilStatus CivilStatus { get; private set; }

    public string? Nationality { get; private set; }

    public string? Citizenship { get; private set; }

    public Guid? PhotographReference { get; private set; }

    public Guid? SignatureReference { get; private set; }

    public EmailAddress? PersonalEmail { get; private set; }

    public EmailAddress? CompanyEmail { get; private set; }

    public PhoneNumber? MobileNumber { get; private set; }

    public PhoneNumber? TelephoneNumber { get; private set; }

    public Address? HomeAddress { get; private set; }

    public Address? MailingAddress { get; private set; }

    public Tin? Tin { get; private set; }

    public SssNumber? Sss { get; private set; }

    public PhilHealthNumber? PhilHealth { get; private set; }

    public PagIbigNumber? PagIbig { get; private set; }

    public GsisNumber? Gsis { get; private set; }

    public BankAccount? Banking { get; private set; }

    public EmployeeLifecycleStage LifecycleStage { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public IReadOnlyList<EmergencyContact> EmergencyContacts => _emergencyContacts.AsReadOnly();

    public IReadOnlyList<FamilyMember> FamilyMembers => _familyMembers.AsReadOnly();

    private Employee(EmployeeId id, Guid tenantId, EmployeeNumber number, DateTimeOffset createdAtUtc)
        : base(id)
    {
        TenantId = tenantId;
        Number = number;
        LifecycleStage = EmployeeLifecycleStage.Hired;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Creates a new Employee starting at <see cref="EmployeeLifecycleStage.Hired"/>
    /// (employee-lifecycle.md: "Hired: Employee Number assigned. Employee Profile
    /// created"). Hire Date, Employment Type, Organization, and Position are
    /// deliberately not accepted here -- commands.md's own "Employee Registration"
    /// section: "hiring is a coordinated business process that creates both an
    /// Employee and its first Employment", the latter supplied separately to the
    /// Employment module's own <c>CreateEmploymentCommand</c> by the orchestrating
    /// caller, never through a compile-time reference from this Aggregate.
    /// </summary>
    public static Result<Employee> Create(
        EmployeeId id, Guid tenantId, string? number, string? firstName, string? middleName, string? lastName,
        string? prefix, string? suffix, string? preferredName, DateOnly dateOfBirth, string? birthPlace, Gender gender,
        CivilStatus civilStatus, string? nationality, string? citizenship, DateTimeOffset nowUtc)
    {
        var numberResult = EmployeeNumber.Create(number);
        if (numberResult.IsFailure)
        {
            return Result.Failure<Employee>(numberResult.Error);
        }

        var nameResult = PersonName.Create(firstName, middleName, lastName, prefix, suffix, preferredName);
        if (nameResult.IsFailure)
        {
            return Result.Failure<Employee>(nameResult.Error);
        }

        if (dateOfBirth > DateOnly.FromDateTime(nowUtc.UtcDateTime))
        {
            return Result.Failure<Employee>(EmployeeErrors.DateOfBirthInFuture);
        }

        var employee = new Employee(id, tenantId, numberResult.Value, nowUtc)
        {
            Name = nameResult.Value,
            DateOfBirth = dateOfBirth,
            BirthPlace = Normalize(birthPlace),
            Gender = gender,
            CivilStatus = civilStatus,
            Nationality = Normalize(nationality),
            Citizenship = Normalize(citizenship),
        };

        employee.AddDomainEvent(new EmployeeCreated(
            Guid.NewGuid(), nowUtc, id, tenantId, numberResult.Value.Value, nameResult.Value.FirstName, nameResult.Value.LastName));

        return Result.Success(employee);
    }

    public Result StartOnboarding(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmployeeLifecycleStage.Hired)
        {
            return Result.Failure(EmployeeErrors.EmployeeNotHired);
        }

        LifecycleStage = EmployeeLifecycleStage.Onboarding;
        AddDomainEvent(new EmployeeOnboardingStarted(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Activate(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmployeeLifecycleStage.Onboarding)
        {
            return Result.Failure(EmployeeErrors.EmployeeNotOnboarding);
        }

        LifecycleStage = EmployeeLifecycleStage.Active;
        AddDomainEvent(new EmployeeActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result StartOffboarding(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmployeeLifecycleStage.Active)
        {
            return Result.Failure(EmployeeErrors.EmployeeNotActive);
        }

        LifecycleStage = EmployeeLifecycleStage.Offboarding;
        AddDomainEvent(new EmployeeOffboardingStarted(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Reaches the Lifecycle Stage <see cref="EmployeeLifecycleStage.Separated"/>.
    /// Per domain-events.md's own EmployeeSeparated remarks, the real trigger for
    /// this in production is "when no Employment remains active" -- observed by
    /// subscribing to the Employment module's own Integration Events, which this
    /// Sprint does not wire (see <see cref="EmployeeHistory"/>'s own remarks on why
    /// event-subscriber infrastructure is out of scope). This method is exposed
    /// directly so the capability exists and is tested; the automatic cross-module
    /// trigger is a tracked, documented gap, not a silently invented shortcut.
    /// </summary>
    public Result Separate(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmployeeLifecycleStage.Offboarding)
        {
            return Result.Failure(EmployeeErrors.EmployeeNotOffboarding);
        }

        LifecycleStage = EmployeeLifecycleStage.Separated;
        AddDomainEvent(new EmployeeSeparated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result Retire(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmployeeLifecycleStage.Separated)
        {
            return Result.Failure(EmployeeErrors.EmployeeNotSeparated);
        }

        LifecycleStage = EmployeeLifecycleStage.Retired;
        AddDomainEvent(new EmployeeRetired(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Reactivates a formerly Separated Employee for a new employment relationship
    /// (commands.md's RehireEmployeeCommand: "rehire is initiated from the
    /// Employee's own Lifecycle Stage reactivation (Separated -&gt; Active)" --
    /// direct, not routed back through Onboarding). Raises the same
    /// <see cref="EmployeeActivated"/> event <see cref="Activate"/> does, since
    /// domain-events.md names no separate rehire-specific event for Employee
    /// (unlike the Employment module's own distinct <c>EmployeeRehired</c> event on
    /// the new Employment record itself, created separately by the orchestrating
    /// caller).
    /// </summary>
    public Result Rehire(DateTimeOffset nowUtc)
    {
        if (LifecycleStage != EmployeeLifecycleStage.Separated)
        {
            return Result.Failure(EmployeeErrors.EmployeeNotSeparated);
        }

        LifecycleStage = EmployeeLifecycleStage.Active;
        AddDomainEvent(new EmployeeActivated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Records the employee's death (BR-EMP-020: "Deceased employees cannot
    /// transition to another lifecycle stage" is the only hard constraint --
    /// reachable from any other stage, since death is discoverable at any point in
    /// the employment journey, not gated behind reaching Offboarding/Separated
    /// first).
    /// </summary>
    public Result RecordDeceased(DateTimeOffset nowUtc)
    {
        if (LifecycleStage == EmployeeLifecycleStage.Deceased)
        {
            return Result.Failure(EmployeeErrors.EmployeeAlreadyDeceased);
        }

        LifecycleStage = EmployeeLifecycleStage.Deceased;
        AddDomainEvent(new EmployeeDeceased(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result UpdatePersonalInformation(
        string? firstName, string? middleName, string? lastName, string? prefix, string? suffix, string? preferredName,
        DateOnly dateOfBirth, string? birthPlace, Gender gender, CivilStatus civilStatus, string? nationality,
        string? citizenship, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        var nameResult = PersonName.Create(firstName, middleName, lastName, prefix, suffix, preferredName);
        if (nameResult.IsFailure)
        {
            return Result.Failure(nameResult.Error);
        }

        if (dateOfBirth > DateOnly.FromDateTime(nowUtc.UtcDateTime))
        {
            return Result.Failure(EmployeeErrors.DateOfBirthInFuture);
        }

        Name = nameResult.Value;
        DateOfBirth = dateOfBirth;
        BirthPlace = Normalize(birthPlace);
        Gender = gender;
        CivilStatus = civilStatus;
        Nationality = Normalize(nationality);
        Citizenship = Normalize(citizenship);

        AddDomainEvent(new EmployeePersonalInformationUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result UpdatePhoto(Guid? photographReference, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        PhotographReference = photographReference;
        AddDomainEvent(new EmployeePersonalInformationUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result UpdateContactInformation(
        string? personalEmail, string? companyEmail, string? mobileNumber, string? telephoneNumber,
        string? homeAddressLine1, string? homeAddressLine2, string? homeCity, string? homeProvince, string? homePostalCode,
        string? homeCountry, string? mailingAddressLine1, string? mailingAddressLine2, string? mailingCity,
        string? mailingProvince, string? mailingPostalCode, string? mailingCountry, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        EmailAddress? personalEmailValue = null;
        if (!string.IsNullOrWhiteSpace(personalEmail))
        {
            var result = EmailAddress.Create(personalEmail);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            personalEmailValue = result.Value;
        }

        EmailAddress? companyEmailValue = null;
        if (!string.IsNullOrWhiteSpace(companyEmail))
        {
            var result = EmailAddress.Create(companyEmail);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            companyEmailValue = result.Value;
        }

        PhoneNumber? mobileValue = null;
        if (!string.IsNullOrWhiteSpace(mobileNumber))
        {
            var result = PhoneNumber.Create(mobileNumber);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            mobileValue = result.Value;
        }

        PhoneNumber? telephoneValue = null;
        if (!string.IsNullOrWhiteSpace(telephoneNumber))
        {
            var result = PhoneNumber.Create(telephoneNumber);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            telephoneValue = result.Value;
        }

        Address? homeAddressValue = null;
        if (!string.IsNullOrWhiteSpace(homeAddressLine1))
        {
            var result = Address.Create(homeAddressLine1, homeAddressLine2, homeCity, homeProvince, homePostalCode, homeCountry);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            homeAddressValue = result.Value;
        }

        Address? mailingAddressValue = null;
        if (!string.IsNullOrWhiteSpace(mailingAddressLine1))
        {
            var result = Address.Create(
                mailingAddressLine1, mailingAddressLine2, mailingCity, mailingProvince, mailingPostalCode, mailingCountry);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            mailingAddressValue = result.Value;
        }

        PersonalEmail = personalEmailValue;
        CompanyEmail = companyEmailValue;
        MobileNumber = mobileValue;
        TelephoneNumber = telephoneValue;
        HomeAddress = homeAddressValue;
        MailingAddress = mailingAddressValue;

        AddDomainEvent(new EmployeeContactInformationUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result UpdateGovernmentInformation(
        string? tin, string? sss, string? philHealth, string? pagIbig, string? gsis, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        Tin? tinValue = null;
        if (!string.IsNullOrWhiteSpace(tin))
        {
            var result = Tin.Create(tin);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            tinValue = result.Value;
        }

        SssNumber? sssValue = null;
        if (!string.IsNullOrWhiteSpace(sss))
        {
            var result = SssNumber.Create(sss);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            sssValue = result.Value;
        }

        PhilHealthNumber? philHealthValue = null;
        if (!string.IsNullOrWhiteSpace(philHealth))
        {
            var result = PhilHealthNumber.Create(philHealth);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            philHealthValue = result.Value;
        }

        PagIbigNumber? pagIbigValue = null;
        if (!string.IsNullOrWhiteSpace(pagIbig))
        {
            var result = PagIbigNumber.Create(pagIbig);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            pagIbigValue = result.Value;
        }

        GsisNumber? gsisValue = null;
        if (!string.IsNullOrWhiteSpace(gsis))
        {
            var result = GsisNumber.Create(gsis);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            gsisValue = result.Value;
        }

        Tin = tinValue;
        Sss = sssValue;
        PhilHealth = philHealthValue;
        PagIbig = pagIbigValue;
        Gsis = gsisValue;

        AddDomainEvent(new EmployeeGovernmentInformationUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    public Result UpdateBankingInformation(
        string? bankName, string? branch, string? accountName, string? accountNumber, string? swiftCode, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        BankAccount? bankingValue = null;
        if (!string.IsNullOrWhiteSpace(bankName) || !string.IsNullOrWhiteSpace(accountNumber))
        {
            var result = BankAccount.Create(bankName, branch, accountName, accountNumber, swiftCode);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }

            bankingValue = result.Value;
        }

        Banking = bankingValue;
        AddDomainEvent(new EmployeeBankingInformationUpdated(Guid.NewGuid(), nowUtc, Id));
        return Result.Success();
    }

    /// <summary>
    /// Adds an emergency contact. When <paramref name="isPrimary"/> is
    /// <see langword="true"/>, any existing primary contact is demoted first
    /// (BR-EMP-036: "at most one emergency contact may be designated as the
    /// primary contact").
    /// </summary>
    public Result AddEmergencyContact(string? name, string? relationship, string? phone, string? email, bool isPrimary, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(EmployeeErrors.EmergencyContactNameRequired);
        }

        if (string.IsNullOrWhiteSpace(relationship))
        {
            return Result.Failure(EmployeeErrors.EmergencyContactRelationshipRequired);
        }

        var phoneResult = PhoneNumber.Create(phone);
        if (phoneResult.IsFailure)
        {
            return Result.Failure(phoneResult.Error);
        }

        EmailAddress? emailValue = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailResult = EmailAddress.Create(email);
            if (emailResult.IsFailure)
            {
                return Result.Failure(emailResult.Error);
            }

            emailValue = emailResult.Value;
        }

        if (isPrimary)
        {
            foreach (var existing in _emergencyContacts.Where(contact => contact.IsPrimary))
            {
                existing.MarkNotPrimary();
            }
        }

        var contact = new EmergencyContact(
            new EmergencyContactId(Guid.NewGuid()), name.Trim(), relationship.Trim(), phoneResult.Value, emailValue, isPrimary, nowUtc);
        _emergencyContacts.Add(contact);

        AddDomainEvent(new EmergencyContactAdded(Guid.NewGuid(), nowUtc, Id, contact.Id));
        return Result.Success();
    }

    public Result UpdateEmergencyContact(
        Guid emergencyContactId, string? name, string? relationship, string? phone, string? email, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        var contact = _emergencyContacts.SingleOrDefault(c => c.Id.Value == emergencyContactId);
        if (contact is null)
        {
            return Result.Failure(EmployeeErrors.EmergencyContactNotFound);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(EmployeeErrors.EmergencyContactNameRequired);
        }

        if (string.IsNullOrWhiteSpace(relationship))
        {
            return Result.Failure(EmployeeErrors.EmergencyContactRelationshipRequired);
        }

        var phoneResult = PhoneNumber.Create(phone);
        if (phoneResult.IsFailure)
        {
            return Result.Failure(phoneResult.Error);
        }

        EmailAddress? emailValue = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailResult = EmailAddress.Create(email);
            if (emailResult.IsFailure)
            {
                return Result.Failure(emailResult.Error);
            }

            emailValue = emailResult.Value;
        }

        contact.UpdateDetails(name.Trim(), relationship.Trim(), phoneResult.Value, emailValue);
        AddDomainEvent(new EmergencyContactUpdated(Guid.NewGuid(), nowUtc, Id, contact.Id));
        return Result.Success();
    }

    public Result RemoveEmergencyContact(Guid emergencyContactId, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        var contact = _emergencyContacts.SingleOrDefault(c => c.Id.Value == emergencyContactId);
        if (contact is null)
        {
            return Result.Failure(EmployeeErrors.EmergencyContactNotFound);
        }

        _emergencyContacts.Remove(contact);
        AddDomainEvent(new EmergencyContactRemoved(Guid.NewGuid(), nowUtc, Id, contact.Id));
        return Result.Success();
    }

    public Result AddFamilyMember(
        string? name, FamilyRelationship relationship, DateOnly? dateOfBirth, bool isDependent, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(EmployeeErrors.FamilyMemberNameRequired);
        }

        var member = new FamilyMember(new FamilyMemberId(Guid.NewGuid()), name.Trim(), relationship, dateOfBirth, isDependent, nowUtc);
        _familyMembers.Add(member);

        AddDomainEvent(new FamilyMemberAdded(Guid.NewGuid(), nowUtc, Id, member.Id));
        return Result.Success();
    }

    public Result UpdateFamilyMember(
        Guid familyMemberId, string? name, FamilyRelationship relationship, DateOnly? dateOfBirth, bool isDependent, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        var member = _familyMembers.SingleOrDefault(m => m.Id.Value == familyMemberId);
        if (member is null)
        {
            return Result.Failure(EmployeeErrors.FamilyMemberNotFound);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(EmployeeErrors.FamilyMemberNameRequired);
        }

        member.UpdateDetails(name.Trim(), relationship, dateOfBirth, isDependent);
        AddDomainEvent(new FamilyMemberUpdated(Guid.NewGuid(), nowUtc, Id, member.Id));
        return Result.Success();
    }

    public Result RemoveFamilyMember(Guid familyMemberId, DateTimeOffset nowUtc)
    {
        var readOnlyCheck = EnsureNotReadOnly();
        if (readOnlyCheck.IsFailure)
        {
            return readOnlyCheck;
        }

        var member = _familyMembers.SingleOrDefault(m => m.Id.Value == familyMemberId);
        if (member is null)
        {
            return Result.Failure(EmployeeErrors.FamilyMemberNotFound);
        }

        _familyMembers.Remove(member);
        AddDomainEvent(new FamilyMemberRemoved(Guid.NewGuid(), nowUtc, Id, member.Id));
        return Result.Success();
    }

    /// <summary>
    /// employee-lifecycle.md's own Separated characteristics: "Employee record
    /// becomes read-only... No operational transactions permitted" -- extended here
    /// to <see cref="EmployeeLifecycleStage.Retired"/> and
    /// <see cref="EmployeeLifecycleStage.Deceased"/> as later stages reachable only
    /// after Separated, so both inherit the same read-only characteristic. Lifecycle
    /// progression methods (<see cref="Retire"/>, <see cref="Rehire"/>,
    /// <see cref="RecordDeceased"/>) intentionally do not call this guard --
    /// progressing out of a read-only stage is the one operation still permitted.
    /// </summary>
    private Result EnsureNotReadOnly() =>
        LifecycleStage is EmployeeLifecycleStage.Separated or EmployeeLifecycleStage.Retired or EmployeeLifecycleStage.Deceased
            ? Result.Failure(EmployeeErrors.EmployeeRecordIsReadOnly)
            : Result.Success();

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
