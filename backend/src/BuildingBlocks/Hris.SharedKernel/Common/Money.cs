namespace Hris.SharedKernel;

/// <summary>
/// Grounded in docs/02-architecture/04-domain-driven-design/value-objects.md's
/// Financial section: "Money... Contains: Amount, Currency... Never use decimal
/// directly for business money." <see cref="Amount"/> is <see cref="decimal"/>,
/// never <c>float</c>/<c>double</c>, per that document's own Implementation
/// Guidance and `CTR-PAY-003`.
///
/// <see cref="Add"/>/<see cref="Subtract"/> return <see cref="Result{TValue}"/>
/// rather than throwing on a currency mismatch: combining PHP and USD amounts is an
/// expected business-input error a caller (payroll aggregating components across a
/// multi-currency benefit, for instance) needs to handle, not a programmer
/// precondition violation (result-pattern.md, "Result vs Exception").
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }

    public CurrencyCode Currency { get; }

    private Money(decimal amount, CurrencyCode currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, CurrencyCode currency)
    {
        Guard.AgainstNull(currency, nameof(currency));
        return new Money(amount, currency);
    }

    public static Money Zero(CurrencyCode currency) => Create(0m, currency);

    public Result<Money> Add(Money other)
    {
        Guard.AgainstNull(other, nameof(other));

        return Currency != other.Currency
            ? Result.Failure<Money>(SharedKernelErrors.MoneyCurrencyMismatch)
            : Result.Success(Create(Amount + other.Amount, Currency));
    }

    public Result<Money> Subtract(Money other)
    {
        Guard.AgainstNull(other, nameof(other));

        return Currency != other.Currency
            ? Result.Failure<Money>(SharedKernelErrors.MoneyCurrencyMismatch)
            : Result.Success(Create(Amount - other.Amount, Currency));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
