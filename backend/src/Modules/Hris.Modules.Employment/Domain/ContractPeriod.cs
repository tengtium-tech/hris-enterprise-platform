using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The effective date range of an <see cref="EmploymentContract"/>. Source:
/// docs/04-modules/employment/domain/value-objects.md, ContractPeriod. Regular
/// (Indefinite) contracts carry a null <see cref="EndDate"/>; every other Contract
/// Type must carry one (CON-005, employment-types.md's own "fixed-term type without
/// a contract end date is invalid").
/// </summary>
public sealed class ContractPeriod : ValueObject
{
    public DateOnly StartDate { get; }

    public DateOnly? EndDate { get; }

    private ContractPeriod(DateOnly startDate, DateOnly? endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static Result<ContractPeriod> Create(DateOnly startDate, DateOnly? endDate)
    {
        if (endDate.HasValue && endDate.Value < startDate)
        {
            return Result.Failure<ContractPeriod>(EmploymentErrors.ContractPeriodEndBeforeStart);
        }

        return Result.Success(new ContractPeriod(startDate, endDate));
    }

    public bool Contains(DateOnly date) => date >= StartDate && (!EndDate.HasValue || date <= EndDate.Value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }

    public override string ToString() => EndDate.HasValue ? $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}" : $"{StartDate:yyyy-MM-dd}, open-ended";
}
