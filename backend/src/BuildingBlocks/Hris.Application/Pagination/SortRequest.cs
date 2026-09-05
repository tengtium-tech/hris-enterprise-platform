namespace Hris.Application.Pagination;

/// <summary>
/// api-standards.md's own Pagination section: "Comma-separated field names; a
/// leading - denotes descending (sort=-createdAt,lastName)." This parses the wire
/// format only -- which field names are actually sortable on a given endpoint is
/// that endpoint's own concern, not something this shared type can validate, since
/// the valid field set differs per resource.
/// </summary>
public sealed record SortField(string Name, bool Descending);

public static class SortRequest
{
    public static IReadOnlyList<SortField> Parse(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return Array.Empty<SortField>();
        }

        return sort
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(field => field.StartsWith('-')
                ? new SortField(field[1..], Descending: true)
                : new SortField(field, Descending: false))
            .ToList();
    }
}
