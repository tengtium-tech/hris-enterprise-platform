namespace Hris.Modules.Workflow.Domain;

/// <summary>
/// A ceiling on what a role may approve, optionally narrowed to one organizational
/// scope. Source: docs/04-modules/workflow/domain/aggregates.md's own
/// <c>ApprovalPolicy</c> "Owns" list ("Approval authority limits by role or scope")
/// and WR-031.
///
/// <see cref="MaximumAmount"/> is deliberately unit-agnostic. No document in this
/// repository states what an authority limit is denominated in, and the plausible
/// readings differ (a monetary ceiling on a compensation change, a day count on a
/// leave request, a percentage on a salary adjustment). Rather than invent one
/// reading and bake it into the model, the value is carried as a decimal the
/// invoking business process interprets against its own request, which keeps the
/// limit expressible for every process without this module having to know any of
/// them. The gap is tracked in STATUS.md.
///
/// A plain record with no identity, so a policy's limits persist as a single
/// JSON-serialized column rather than an owned collection.
/// </summary>
/// <param name="RoleName">The role the ceiling applies to.</param>
/// <param name="ScopeId">Where set, narrows the ceiling to one organizational scope.</param>
/// <param name="MaximumAmount">The ceiling itself, interpreted by the invoking business process.</param>
public sealed record ApprovalAuthorityLimit(string RoleName, Guid? ScopeId, decimal MaximumAmount);
