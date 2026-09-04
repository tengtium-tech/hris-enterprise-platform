using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// Aggregate Root registering one government-published program within one country's own
/// scope -- statutory-reference-data.md's own Country Scoping tree ("Philippines -&gt;
/// SSS, PhilHealth, Pag-IBIG, BIR withholding, Regional minimum wage") and Government
/// Sector Variance ("GSIS is represented as a separate statutory program within the
/// Philippine country scope -- not as a conditional branch inside private-sector
/// logic"). Eighth and last framework built in Sprint 4.
///
/// Deliberately carries no <c>TenantId</c> field, unlike Search/Scheduling/Job
/// Processing (Sprint 4's three immediately preceding frameworks), which each build
/// CTR-ISO-00x tenant context concretely -- this framework's own document is explicit
/// and unambiguous in the opposite direction: Security Considerations states "Statutory
/// Reference Data is readable by every tenant; it is public information" and "Reference
/// data is excluded from tenant data export, since it is not tenant data." Adding a
/// TenantId here would misrepresent this aggregate as tenant-scoped data when the
/// document's own governing principle ("Statutory rates are law, not configuration") is
/// specifically that no tenant owns or varies it.
///
/// Owns no configurable "policy" field the way <c>JobQueue.MaxConcurrency</c> or
/// <c>NumberSeries</c>' own format do -- this document gives no basis for a program's
/// own identity fields (<see cref="Code"/>, <see cref="Country"/>) being editable after
/// registration, so unlike every other Sprint 4 config aggregate, this one has no
/// <c>Update</c> method at all. A version's own scheduled/rate content is what actually
/// changes over time, and that lives on <see cref="StatutoryTableVersion"/>, never here.
/// </summary>
public sealed class StatutoryProgram : AggregateRoot<StatutoryProgramId>
{
    public StatutoryProgramCode Code { get; }

    public StatutoryCountryCode Country { get; }

    public string DisplayName { get; }

    public DateTimeOffset RegisteredAtUtc { get; }

    private StatutoryProgram(
        StatutoryProgramId id,
        StatutoryProgramCode code,
        StatutoryCountryCode country,
        string displayName,
        DateTimeOffset registeredAtUtc)
        : base(id)
    {
        Code = code;
        Country = country;
        DisplayName = displayName;
        RegisteredAtUtc = registeredAtUtc;
    }

    /// <summary>
    /// Registers a new program. Uniqueness of <paramref name="code"/> within
    /// <paramref name="country"/> is checked by the caller before this factory runs
    /// (<see cref="IStatutoryProgramRepository.ExistsByCodeAndCountryAsync"/>), not here
    /// -- the same split every other uniqueness-checked factory in this codebase
    /// establishes (<c>Tenant.Register</c>, <c>JobQueue.Register</c>,
    /// <c>SearchIndexDefinition.Register</c>). Raises no event, per
    /// <see cref="StatutoryReferenceDataEvents"/>'s own remarks.
    /// </summary>
    public static Result<StatutoryProgram> Register(
        StatutoryProgramCode code,
        StatutoryCountryCode country,
        string? displayName,
        DateTimeOffset registeredAtUtc)
    {
        Guard.AgainstNull(code, nameof(code));
        Guard.AgainstNull(country, nameof(country));

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return Result.Failure<StatutoryProgram>(StatutoryReferenceDataErrors.DisplayNameRequired);
        }

        var program = new StatutoryProgram(
            new StatutoryProgramId(Guid.NewGuid()), code, country, displayName.Trim(), registeredAtUtc);

        return Result.Success(program);
    }
}
