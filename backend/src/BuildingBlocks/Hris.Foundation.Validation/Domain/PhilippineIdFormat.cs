namespace Hris.Foundation.Validation.Domain;

/// <summary>
/// Shared digit-extraction helper for the four Philippine government identifier
/// value objects in this namespace, so the same hyphen/space-stripping rule is not
/// repeated four times and cannot drift between them.
/// </summary>
internal static class PhilippineIdFormat
{
    public static string? ExtractDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());
        var nonDigitNonSeparator = value.Any(c => !char.IsAsciiDigit(c) && c != '-' && c != ' ');

        return nonDigitNonSeparator ? null : digits;
    }
}
