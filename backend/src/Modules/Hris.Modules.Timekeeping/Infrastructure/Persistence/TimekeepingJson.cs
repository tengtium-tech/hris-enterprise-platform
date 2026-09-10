using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Hris.Modules.Timekeeping.Infrastructure.Persistence;

/// <summary>
/// Shared helpers for this module's JSON-column mappings.
///
/// Four separate collections in this module hold values with no identity of their own
/// — working days, break rules, split shift periods — and every one needs the same
/// three pieces: a serialize expression, a deserialize expression, and a value
/// comparer, because EF Core cannot compare two deserialized list instances by
/// reference for change tracking. Writing that trio out four times invites one of
/// them to drift; the Administration module wrote it twice and this module would
/// have written it four more.
/// </summary>
internal static class TimekeepingJson
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
}
