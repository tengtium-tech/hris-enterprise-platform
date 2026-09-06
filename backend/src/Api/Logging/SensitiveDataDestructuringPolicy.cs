using System.Collections.Concurrent;
using System.Reflection;
using Hris.SharedKernel;
using Serilog.Core;
using Serilog.Events;

namespace Hris.Api.Logging;

/// <summary>
/// monitoring-and-alerting.md's own Structured Logging section (NFR-OB-001): "a
/// Serilog destructuring policy (or equivalent enricher) applied to types known to
/// carry sensitive data... redacts or omits the sensitive fields before the log event
/// is emitted, so that a developer who logs an entire object for debugging
/// convenience does not thereby leak the data NFR-OB-001 prohibits." This is that
/// policy: any property marked <see cref="SensitiveDataAttribute"/> is replaced with a
/// fixed redaction placeholder wherever the object is logged via Serilog's own
/// destructuring (<c>{@Something}</c>) syntax; every other property destructures
/// normally.
///
/// Structural, not conventional -- per this document's own reasoning: "A code-review
/// checklist item alone would rely on every reviewer remembering this on every
/// change." A call site that logs an entire sensitive object for debugging
/// convenience gets the redacted property automatically, with no per-call-site
/// opt-in required.
///
/// Reflected property lists are cached per <see cref="Type"/> (<see cref="_sensitivePropertiesByType"/>)
/// since Serilog's own destructuring runs on the hot logging path and reflection
/// itself is not free -- the identical "compute once, cache by type" reasoning any
/// reflection-heavy hot path in this codebase already follows.
/// </summary>
internal sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private const string _redactedPlaceholder = "***REDACTED***";

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<PropertyInfo>> _sensitivePropertiesByType = new();

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(propertyValueFactory);

        var type = value.GetType();
        var sensitiveProperties = _sensitivePropertiesByType.GetOrAdd(type, FindSensitiveProperties);

        if (sensitiveProperties.Count == 0)
        {
            // No property on this type is marked SensitiveDataAttribute -- defer to
            // Serilog's own default destructuring rather than reimplementing it here.
            // Null-forgiving: Serilog's own TryDestructure contract treats `result` as
            // unused whenever the return value is false, the same as any other
            // TryXxx pattern in .NET (e.g. int.TryParse) -- this is the value the
            // interface itself is declared to expect on this path, not a genuine
            // possible-null value being smuggled past the compiler.
            result = null!;
            return false;
        }

        var properties = new List<LogEventProperty>();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var propertyValue = sensitiveProperties.Contains(property)
                ? new ScalarValue(_redactedPlaceholder)
                : propertyValueFactory.CreatePropertyValue(property.GetValue(value), destructureObjects: true);

            properties.Add(new LogEventProperty(property.Name, propertyValue));
        }

        result = new StructureValue(properties, type.Name);
        return true;
    }

    private static IReadOnlyList<PropertyInfo> FindSensitiveProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<SensitiveDataAttribute>() is not null)
            .ToList();
}
