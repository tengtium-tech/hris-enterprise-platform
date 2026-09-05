namespace Hris.Foundation.Entitlement.Application.Dtos;

/// <summary>
/// The read-side shape of one <see cref="Domain.ProcessPackCode"/> catalogue entry,
/// returned by <c>ListProcessPacksQuery</c>.
/// </summary>
public sealed record ProcessPackDto(
    string Code,
    string DisplayName,
    bool IsCore,
    IReadOnlyCollection<string> ConditionalDependencies);
