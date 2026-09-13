using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hris.Modules.Attendance.Infrastructure.Persistence;

/// <summary>
/// Shared helpers for this module's JSON-column mappings, mirroring the pattern
/// Timekeeping's <c>TimekeepingJson</c> established: a serialize expression, a deserialize
/// expression, and a value comparer for every value object or collection of values without
/// identity (the policy configuration, device location, device configuration, biometric
/// template reference, approval decision, applied adjustments, exceptions, synchronized
/// device ids, supporting documents). EF Core cannot compare two deserialized instances by
/// reference for change tracking, so each needs all three.
/// </summary>
internal static class AttendanceJson
{
    public static Expression<Func<IReadOnlyList<T>, string>> To<T>() =>
        items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null);

    public static Expression<Func<string, IReadOnlyList<T>>> From<T>() =>
        json => JsonSerializer.Deserialize<List<T>>(json, (JsonSerializerOptions?)null) ?? new List<T>();

    public static ValueComparer<IReadOnlyList<T>> ComparerFor<T>() =>
        new(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            items => (IReadOnlyList<T>)items.ToList());

    public static Expression<Func<T, string>> ToValue<T>(bool _ = false) =>
        value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null);

    public static Expression<Func<string, T>> FromValue<T>() =>
        json => JsonSerializer.Deserialize<T>(json, (JsonSerializerOptions?)null)!;

    public static ValueComparer<T> ValueComparer<T>() =>
        new(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            value => value);

    // Expression<Func<...>> bodies below use == / != rather than the `is null` / `is not
    // null` pattern-matching forms used everywhere else in this file's non-expression
    // code: an expression tree cannot represent a pattern-matching operator (CS8122),
    // only the older equality-operator form, even though the two are semantically
    // identical for a reference-type null check.
    public static Expression<Func<T?, string>> ToNullable<T>() where T : class =>
        value => value == null ? "null" : JsonSerializer.Serialize(value, (JsonSerializerOptions?)null);

    public static Expression<Func<string, T?>> FromNullable<T>() where T : class =>
        json => string.Equals(json, "null", StringComparison.Ordinal)
            ? null
            : JsonSerializer.Deserialize<T>(json, (JsonSerializerOptions?)null);

    public static ValueComparer<T?> NullableComparer<T>() where T : class =>
        new(
            (left, right) => (left == null && right == null)
                || (left != null && right != null
                    && JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null)),
            value => value == null ? 0 : JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            value => value);
}
