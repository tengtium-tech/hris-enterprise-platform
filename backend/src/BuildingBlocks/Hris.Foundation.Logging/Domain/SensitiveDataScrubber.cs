using Hris.SharedKernel;

namespace Hris.Foundation.Logging.Domain;

/// <summary>
/// Structurally enforces logging-framework.md's Security Considerations: "Passwords,
/// secrets, authentication tokens, and personal financial information should never
/// be written to logs." <see cref="LogEntry.Create"/> routes every entry's metadata
/// through <see cref="Redact"/> before construction, so a caller cannot forget the
/// rule the way a purely documented convention could be forgotten
/// (this project's own engineering principle: "Prefer structure over discipline").
///
/// Redaction here is deliberately key-name-based, applied only to
/// <see cref="LogEntry.Metadata"/>'s structured key/value pairs, not a pattern scan
/// over <see cref="LogEntry.Message"/>'s free text. A scan for sensitive-looking
/// *content* inside arbitrary prose is unreliable in both directions -- it misses
/// what its pattern list does not anticipate and mangles legitimate text that
/// happens to match one -- so this framework's own Implementation Guidance places
/// message hygiene on the caller instead ("Log at a level that permits diagnosis
/// without reproducing sensitive content in the message"). Metadata is different:
/// callers are far more often tempted to dump a whole object's fields there, key
/// names are known in advance, and matching by key is a check that cannot silently
/// miss a field it was told to protect.
/// </summary>
public static class SensitiveDataScrubber
{
    private const string _redactedValue = "***REDACTED***";

    private static readonly string[] _sensitiveKeyFragments =
    [
        "password",
        "secret",
        "token",
        "apikey",
        "creditcard",
        "cvv",
        "pin",
        "ssn",
        "tin",
        "sssnumber",
        "philhealthnumber",
        "pagibignumber",
        "salary",
        "compensation",
        "bankaccount",
    ];

    public static IReadOnlyDictionary<string, string> Redact(IReadOnlyDictionary<string, string> metadata)
    {
        Guard.AgainstNull(metadata, nameof(metadata));

        var redacted = new Dictionary<string, string>(metadata.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in metadata)
        {
            redacted[key] = IsSensitiveKey(key) ? _redactedValue : value;
        }

        return redacted;
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        return _sensitiveKeyFragments.Any(fragment =>
            normalized.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}
