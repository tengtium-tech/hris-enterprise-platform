namespace Hris.SharedKernel;

/// <summary>
/// Grounded in docs/02-architecture/04-domain-driven-design/result-pattern.md's Core
/// Principle: "Every business operation has one of two outcomes: Success, Failure ...
/// Failures are expected business outcomes and should be represented explicitly."
///
/// Reserved exclusively for expected business outcomes (result-pattern.md, "Result vs
/// Exception"); an unexpected technical failure still throws. Never construct a
/// success <see cref="Result"/> carrying a real <see cref="Error"/>, or a failure
/// carrying <see cref="Error.None"/> -- the constructor guards exactly that pairing,
/// per the Common Anti-Patterns this document warns against ("Mixing Result and
/// exception semantics for the same business rule").
///
/// <see cref="Success{TValue}"/> and <see cref="Failure{TValue}"/> are generic
/// *methods* on this non-generic type rather than static members of
/// <see cref="Result{TValue}"/> itself, per CA1000 ("do not declare static members on
/// generic types") -- <c>Result.Success(value)</c> also reads more naturally than
/// <c>Result&lt;TValue&gt;.Success(value)</c>, since the compiler infers
/// <c>TValue</c> from the argument.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("A successful Result cannot carry an Error.", nameof(error));
        }

        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("A failed Result must carry an Error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(true, value, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(false, default, error);
}

/// <summary>
/// The value-carrying form of <see cref="Result"/>, per result-pattern.md's Factory
/// Methods section ("<c>EmployeeFactory -&gt; Validate -&gt; Create Aggregate -&gt;
/// Result&lt;Employee&gt;</c>") and its explicit prohibition on a success result with
/// no payload ("Never return a success result with a null or default payload" -- AI
/// Implementation Guidance). <see cref="Value"/> throws rather than returning
/// <c>default</c> when accessed on a failed <see cref="Result{TValue}"/>, so a caller
/// that forgets to check <see cref="Result.IsSuccess"/> first fails loudly instead of
/// silently receiving <c>null</c> or a zeroed struct.
///
/// Construct through <see cref="Result.Success{TValue}"/> / <see cref="Result.Failure{TValue}"/>
/// only -- the constructor is <c>internal</c> rather than exposing same-shape static
/// factories directly on this generic type, again per CA1000.
/// </summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(bool isSuccess, TValue? value, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failed Result cannot be accessed.");
}
