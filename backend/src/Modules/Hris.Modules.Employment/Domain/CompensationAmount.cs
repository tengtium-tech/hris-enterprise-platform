using Hris.SharedKernel;

namespace Hris.Modules.Employment.Domain;

/// <summary>
/// The base compensation figure recorded on a <see cref="CompensationRecord"/> --
/// amount, currency, and Compensation Basis together. Source:
/// docs/04-modules/employment/domain/value-objects.md's own CompensationAmount
/// ("Pair the amount with a currency rather than assuming a single tenant-wide
/// currency... Carry the Compensation Basis classification already defined in
/// employment-categories.md, rather than duplicating it as a second enumeration").
/// Currency is a plain ISO 4217 code string rather than a shared Currency Value
/// Object, since no such shared type exists yet in Hris.SharedKernel or a built
/// Foundation framework for this module to reuse -- a documented gap, not an
/// invented dependency.
/// </summary>
public sealed class CompensationAmount : ValueObject
{
    public decimal Amount { get; }

    public string CurrencyCode { get; }

    public CompensationBasis Basis { get; }

    private CompensationAmount(decimal amount, string currencyCode, CompensationBasis basis)
    {
        Amount = amount;
        CurrencyCode = currencyCode;
        Basis = basis;
    }

    public static Result<CompensationAmount> Create(decimal amount, string? currencyCode, CompensationBasis basis)
    {
        if (amount < 0)
        {
            return Result.Failure<CompensationAmount>(EmploymentErrors.CompensationAmountNegative);
        }

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
        {
            return Result.Failure<CompensationAmount>(EmploymentErrors.CompensationCurrencyCodeInvalid);
        }

        return Result.Success(new CompensationAmount(amount, currencyCode.Trim().ToUpperInvariant(), basis));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return CurrencyCode;
        yield return Basis;
    }

    public override string ToString() => $"{Amount} {CurrencyCode} ({Basis})";
}
