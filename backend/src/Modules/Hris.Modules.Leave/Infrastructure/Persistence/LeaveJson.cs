using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hris.Modules.Leave.Infrastructure.Persistence;

/// <summary>
/// Shared helpers for this module's JSON-column mappings, mirroring the pattern
/// <c>Hris.Modules.Attendance</c>'s own <c>AttendanceJson</c> established: a serialize
/// expression, a deserialize expression, and a value comparer for every value object
/// without identity. EF Core cannot compare two deserialized instances by reference for
/// change tracking, so each needs all three.
/// </summary>
internal static class LeaveJson
{
    public static Expression<Func<T, string>> ToValue<T>() =>
        value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null);

    public static Expression<Func<string, T>> FromValue<T>() =>
        json => JsonSerializer.Deserialize<T>(json, (JsonSerializerOptions?)null)!;

    public static ValueComparer<T> ValueComparer<T>() =>
        new(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            value => value);
}
