using Hris.Foundation.Identity.Domain;
using Hris.SharedKernel;

namespace Hris.Foundation.Authorization.Domain;

/// <summary>
/// One grant of one <see cref="Role"/>, at one <see cref="OrganizationalScope"/>, to
/// one principal -- the Aggregate Root behind authorization-framework.md's Role
/// Assignment section. A principal holding several roles (e.g. `Employee` plus
/// `PeopleManager`, per personas.md's own "A user with direct reports holds
/// `PeopleManager` in addition to `Employee`") holds several separate
/// <see cref="RoleAssignment"/> instances, not one aggregate listing many roles --
/// each is independently grantable, revocable, and time-bound
/// (aggregate-design-rules.md Rule 1, one Aggregate per business concept; Rule 4,
/// keep Aggregates small).
///
/// <see cref="PrincipalId"/> is Identity Framework's own <see cref="UserAccountId"/>,
/// referenced by identity per `CTR-ARC-002` -- Identity is this framework's own
/// stated #1 Upstream Dependency and now exists (built earlier in this same Sprint 3),
/// so this is a real <c>ProjectReference</c>, not a forward-reference placeholder.
/// </summary>
public sealed class RoleAssignment : AggregateRoot<RoleAssignmentId>
{
    public UserAccountId PrincipalId { get; }

    public Role Role { get; }

    // null! -- valid for every caller of the public API: the application-facing
    // constructor below always assigns a real value; only the EF-only constructor
    // (see its own remarks) leaves this to be populated post-construction, which the
    // compiler's definite-assignment analysis cannot see coming.
    public OrganizationalScope Scope { get; } = null!;

    public RoleAssignmentType AssignmentType { get; }

    public DateOnly EffectiveDate { get; }

    public DateOnly? ExpirationDate { get; }

    public UserAccountId GrantedByPrincipalId { get; }

    public DateTimeOffset GrantedAtUtc { get; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    private RoleAssignment(
        RoleAssignmentId id,
        UserAccountId principalId,
        Role role,
        OrganizationalScope scope,
        RoleAssignmentType assignmentType,
        DateOnly effectiveDate,
        DateOnly? expirationDate,
        UserAccountId grantedByPrincipalId,
        DateTimeOffset grantedAtUtc)
        : base(id)
    {
        PrincipalId = principalId;
        Role = role;
        Scope = scope;
        AssignmentType = assignmentType;
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
        GrantedByPrincipalId = grantedByPrincipalId;
        GrantedAtUtc = grantedAtUtc;
    }

    /// <summary>
    /// EF Core materialization only -- never called by application code, which always
    /// goes through the constructor above via <see cref="Create"/>. EF Core's own
    /// constructor-binding convention cannot bind <c>scope</c> through a constructor
    /// parameter because <see cref="OrganizationalScope"/> is an owned-type
    /// navigation (<c>RoleAssignmentConfiguration</c>'s own <c>OwnsOne</c>), not a
    /// scalar or converted property ("navigations to related entities, including
    /// references to owned types, cannot be bound," per that convention's own
    /// documented rule); every other parameter above binds fine. Rather than change
    /// the constructor application code actually calls, this second constructor
    /// gives EF Core one that satisfies its own binding rule, and it selects this one
    /// automatically for materialization, since it is the best fully-bindable match.
    /// EF Core then populates <see cref="Scope"/> through this get-only auto-
    /// property's own compiler-generated backing field, the same as any owned
    /// reference with no custom getter body -- see <c>ConfigurationSetting</c>'s own
    /// identical second constructor for the full reasoning.
    ///
    /// Excluded from coverage measurement (NFR-MT-001's own 95% floor): this
    /// constructor carries no logic of its own to verify -- the same field
    /// assignments as the constructor above, minus the one parameter EF Core cannot
    /// bind -- and reflection-only construction by EF Core's own materialization
    /// pipeline is exactly what Hris.Infrastructure.IntegrationTests already proves
    /// works, against a real PostgreSQL instance, not what a Domain-layer unit test
    /// (which never constructs a real HrisDbContext) can exercise or meaningfully
    /// verify.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private RoleAssignment(
        RoleAssignmentId id,
        UserAccountId principalId,
        Role role,
        RoleAssignmentType assignmentType,
        DateOnly effectiveDate,
        DateOnly? expirationDate,
        UserAccountId grantedByPrincipalId,
        DateTimeOffset grantedAtUtc)
        : base(id)
    {
        PrincipalId = principalId;
        Role = role;
        AssignmentType = assignmentType;
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
        GrantedByPrincipalId = grantedByPrincipalId;
        GrantedAtUtc = grantedAtUtc;
    }

    public static Result<RoleAssignment> Create(
        UserAccountId principalId,
        Role role,
        OrganizationalScope scope,
        RoleAssignmentType assignmentType,
        DateOnly effectiveDate,
        DateOnly? expirationDate,
        UserAccountId grantedByPrincipalId,
        DateTimeOffset nowUtc)
    {
        Guard.AgainstNull(scope, nameof(scope));

        if (expirationDate is not null && expirationDate.Value < effectiveDate)
        {
            return Result.Failure<RoleAssignment>(AuthorizationErrors.ExpirationBeforeEffectiveDate);
        }

        var assignment = new RoleAssignment(
            new RoleAssignmentId(Guid.NewGuid()),
            principalId,
            role,
            scope,
            assignmentType,
            effectiveDate,
            expirationDate,
            grantedByPrincipalId,
            nowUtc);

        assignment.AddDomainEvent(new RoleAssigned(Guid.NewGuid(), nowUtc, assignment.Id, principalId, role, scope));
        return Result.Success(assignment);
    }

    /// <summary>
    /// Whether this grant is currently in force: not revoked, and
    /// <paramref name="asOfDate"/> falls within [<see cref="EffectiveDate"/>,
    /// <see cref="ExpirationDate"/>). `CTR-AUT-007` ("Revocation Takes Effect
    /// Immediately") depends on this being re-evaluated on every check rather than
    /// cached on the aggregate itself -- see <see cref="AuthorizationEvaluator"/>,
    /// which must always ask a fresh <see cref="IRoleAssignmentRepository"/> read
    /// this question, never reuse a previously loaded answer.
    /// </summary>
    public bool IsEffective(DateOnly asOfDate) =>
        RevokedAtUtc is null && asOfDate >= EffectiveDate && (ExpirationDate is null || asOfDate < ExpirationDate.Value);

    /// <summary>
    /// Idempotent, matching the retry-safety convention this Sprint's Identity
    /// Framework established for <c>Session.Revoke</c>/<c>MfaFactor.Remove</c>: a
    /// retried revoke against an already-revoked assignment succeeds without raising
    /// a second <see cref="RoleRevoked"/>, rather than surfacing a conflict for a
    /// state the caller already achieved.
    /// </summary>
    public Result Revoke(DateTimeOffset nowUtc)
    {
        if (RevokedAtUtc is not null)
        {
            return Result.Success();
        }

        RevokedAtUtc = nowUtc;
        AddDomainEvent(new RoleRevoked(Guid.NewGuid(), nowUtc, Id, PrincipalId, Role));
        return Result.Success();
    }
}
