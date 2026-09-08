using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// A person's legal name. Source: docs/04-modules/employee/domain/value-objects.md's
/// PersonName ("Components: First Name, Middle Name, Last Name, Prefix, Suffix,
/// Preferred Name... Supports international naming conventions"). Mapped
/// <c>OwnsOne</c> on <see cref="Employee"/>; assigned via object-initializer in
/// <see cref="Employee.Create"/> and <see cref="Employee.UpdatePersonalInformation"/>
/// rather than through <see cref="Employee"/>'s own constructor, per this project's
/// own EF Core constructor-binding rule (a constructor parameter must never bind to
/// an owned navigation) -- see feedback memory feedback-ef-core-constructor-binding.
/// </summary>
public sealed class PersonName : ValueObject
{
    private const int _maxLength = 100;

    public string FirstName { get; }

    public string? MiddleName { get; }

    public string LastName { get; }

    public string? Prefix { get; }

    public string? Suffix { get; }

    public string? PreferredName { get; }

    private PersonName(string firstName, string? middleName, string lastName, string? prefix, string? suffix, string? preferredName)
    {
        FirstName = firstName;
        MiddleName = middleName;
        LastName = lastName;
        Prefix = prefix;
        Suffix = suffix;
        PreferredName = preferredName;
    }

    public static Result<PersonName> Create(
        string? firstName, string? middleName, string? lastName, string? prefix, string? suffix, string? preferredName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return Result.Failure<PersonName>(EmployeeErrors.FirstNameRequired);
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return Result.Failure<PersonName>(EmployeeErrors.LastNameRequired);
        }

        if (firstName.Trim().Length > _maxLength || lastName.Trim().Length > _maxLength
            || (middleName?.Trim().Length ?? 0) > _maxLength || (prefix?.Trim().Length ?? 0) > 20
            || (suffix?.Trim().Length ?? 0) > 20 || (preferredName?.Trim().Length ?? 0) > _maxLength)
        {
            return Result.Failure<PersonName>(EmployeeErrors.PersonNameFieldTooLong);
        }

        return Result.Success(new PersonName(
            firstName.Trim(), Normalize(middleName), lastName.Trim(), Normalize(prefix), Normalize(suffix), Normalize(preferredName)));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return MiddleName;
        yield return LastName;
        yield return Prefix;
        yield return Suffix;
        yield return PreferredName;
    }

    public override string ToString() =>
        string.Join(" ", new[] { Prefix, FirstName, MiddleName, LastName, Suffix }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
