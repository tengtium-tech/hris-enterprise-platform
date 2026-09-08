using Hris.SharedKernel;

namespace Hris.Modules.Employee.Domain;

/// <summary>
/// Payroll banking information. Source:
/// docs/04-modules/employee/domain/value-objects.md's BankAccount ("Components:
/// Bank Name, Branch, Account Name, Account Number, SWIFT Code... Sensitive values
/// encrypted"). Encryption-at-rest is an infrastructure/persistence concern
/// (column-level encryption or a value converter) deferred alongside this Sprint's
/// other "needs a live database to verify" items -- see infrastructure/
/// persistence.md's own guidance and this module's own STATUS.md entry. Assigned
/// via object-initializer, never through <see cref="Employee"/>'s own constructor
/// -- see <see cref="PersonName"/>'s own remarks.
/// </summary>
public sealed class BankAccount : ValueObject
{
    private const int _maxFieldLength = 100;

    public string BankName { get; }

    public string? Branch { get; }

    public string? AccountName { get; }

    public string AccountNumber { get; }

    public string? SwiftCode { get; }

    private BankAccount(string bankName, string? branch, string? accountName, string accountNumber, string? swiftCode)
    {
        BankName = bankName;
        Branch = branch;
        AccountName = accountName;
        AccountNumber = accountNumber;
        SwiftCode = swiftCode;
    }

    public static Result<BankAccount> Create(string? bankName, string? branch, string? accountName, string? accountNumber, string? swiftCode)
    {
        if (string.IsNullOrWhiteSpace(bankName))
        {
            return Result.Failure<BankAccount>(EmployeeErrors.BankNameRequired);
        }

        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return Result.Failure<BankAccount>(EmployeeErrors.BankAccountNumberRequired);
        }

        if (bankName.Trim().Length > _maxFieldLength || (branch?.Trim().Length ?? 0) > _maxFieldLength
            || (accountName?.Trim().Length ?? 0) > _maxFieldLength || accountNumber.Trim().Length > _maxFieldLength
            || (swiftCode?.Trim().Length ?? 0) > 20)
        {
            return Result.Failure<BankAccount>(EmployeeErrors.BankAccountFieldTooLong);
        }

        return Result.Success(new BankAccount(
            bankName.Trim(), Normalize(branch), Normalize(accountName), accountNumber.Trim(), Normalize(swiftCode)));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BankName;
        yield return Branch;
        yield return AccountName;
        yield return AccountNumber;
        yield return SwiftCode;
    }

    public override string ToString() => $"{BankName} ({AccountNumber})";

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
