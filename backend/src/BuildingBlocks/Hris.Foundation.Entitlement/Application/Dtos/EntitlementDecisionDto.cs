namespace Hris.Foundation.Entitlement.Application.Dtos;

/// <summary>
/// The read-side shape of a <see cref="Domain.EntitlementDecision"/>, returned by
/// <c>EvaluateEntitlementQuery</c>. <see cref="DenialReason"/> is <c>null</c> whenever
/// <see cref="IsEntitled"/> is <c>true</c>.
/// </summary>
public sealed record EntitlementDecisionDto(bool IsEntitled, string? DenialReason);
