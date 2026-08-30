namespace Hris.SharedKernel;

/// <summary>
/// Base shape for every strongly typed identifier in the platform (<c>EmployeeId</c>,
/// <c>EmploymentId</c>, <c>TenantId</c>, and so on).
///
/// Grounded in docs/08-devops/coding-standards.md's Domain Layer convention: strongly
/// typed IDs are used at every aggregate boundary so that a method which conceptually
/// takes an <c>EmployeeId</c> cannot silently accept an <c>EmploymentId</c>'s raw
/// <see cref="Guid"/> value by argument-order mistake -- the compiler catches it instead
/// of a production incident. See also
/// docs/02-architecture/05-data-architecture/strongly-typed-id-mapping.md for how this
/// maps to a PostgreSQL <c>uuid</c> column via an EF Core Value Converter (that mapping
/// lives in the Infrastructure layer, per CTR-ARC-001 -- never referenced from here).
///
/// A concrete ID type is a <c>readonly record struct</c> deriving from this record,
/// e.g.:
/// <code>
/// public readonly record struct EmployeeId(Guid Value) : IStronglyTypedId;
/// </code>
/// This is a shape convention, not a generated base class -- each module declares its
/// own ID types this way rather than depending on a shared generic wrapper, so that a
/// module's Domain layer has zero dependency on any other module's own types.
/// </summary>
public interface IStronglyTypedId
{
    Guid Value { get; }
}
