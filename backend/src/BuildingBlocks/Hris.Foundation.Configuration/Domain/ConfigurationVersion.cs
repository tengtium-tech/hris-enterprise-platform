using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// One version of a <see cref="ConfigurationSetting"/>'s value, per
/// configuration-framework.md's Configuration Versioning section: "Version Number,
/// Effective Date, Expiration Date, Change Summary, Approval Information, Previous
/// Version."
///
/// A child Entity of the <see cref="ConfigurationSetting"/> Aggregate, never an
/// Aggregate Root of its own
/// (docs/02-architecture/04-domain-driven-design/aggregate-design-rules.md Rule 7,
/// "Child Entities Never Escape") -- its constructor and every transition method are
/// <c>internal</c>, reachable only through <see cref="ConfigurationSetting"/>'s own
/// methods, never called directly from outside this assembly.
///
/// Approvals are recorded by a raw <see cref="Guid"/> user id rather than a strongly
/// typed <c>UserId</c>: Identity Framework, which will own that type, is built later
/// in this same Sprint 3 Core Kernel (IMPLEMENTATION-PLAN.md's stated bootstrap
/// order), so no such type exists yet. Revisit once it does.
/// </summary>
public sealed class ConfigurationVersion : Entity<ConfigurationVersionId>
{
    public int VersionNumber { get; }

    public string Value { get; }

    public DateOnly EffectiveDate { get; }

    public DateOnly? ExpirationDate { get; }

    public string ChangeSummary { get; }

    public Guid CreatedByUserId { get; }

    public ConfigurationLifecycleState State { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public DateTimeOffset? ApprovedAtUtc { get; private set; }

    internal ConfigurationVersion(
        ConfigurationVersionId id,
        int versionNumber,
        string value,
        DateOnly effectiveDate,
        DateOnly? expirationDate,
        string changeSummary,
        Guid createdByUserId)
        : base(id)
    {
        VersionNumber = versionNumber;
        Value = value;
        EffectiveDate = effectiveDate;
        ExpirationDate = expirationDate;
        ChangeSummary = changeSummary;
        CreatedByUserId = createdByUserId;
        State = ConfigurationLifecycleState.Draft;
    }

    /// <summary>
    /// Whether this version is the one in force on <paramref name="asOfDate"/>,
    /// judged purely from its own effective/expiration dates and from having reached
    /// at least <see cref="ConfigurationLifecycleState.Published"/> -- never from
    /// today's wall-clock date or its current <see cref="State"/> beyond that
    /// threshold. See <see cref="ConfigurationSetting.GetValueAsOf"/>, which this
    /// supports.
    /// </summary>
    internal bool IsInForceOn(DateOnly asOfDate)
    {
        if (State < ConfigurationLifecycleState.Published)
        {
            return false;
        }

        if (asOfDate < EffectiveDate)
        {
            return false;
        }

        return ExpirationDate is null || asOfDate < ExpirationDate.Value;
    }

    internal Result MarkValidated()
    {
        if (State != ConfigurationLifecycleState.Draft)
        {
            return Result.Failure(ConfigurationErrors.InvalidLifecycleTransition);
        }

        State = ConfigurationLifecycleState.Validated;
        return Result.Success();
    }

    internal Result Approve(Guid approverId, DateTimeOffset approvedAtUtc)
    {
        if (State != ConfigurationLifecycleState.Validated)
        {
            return Result.Failure(ConfigurationErrors.InvalidLifecycleTransition);
        }

        State = ConfigurationLifecycleState.Approved;
        ApprovedByUserId = approverId;
        ApprovedAtUtc = approvedAtUtc;
        return Result.Success();
    }

    internal Result Publish()
    {
        if (State != ConfigurationLifecycleState.Approved)
        {
            return Result.Failure(ConfigurationErrors.InvalidLifecycleTransition);
        }

        State = ConfigurationLifecycleState.Published;
        return Result.Success();
    }

    internal Result Activate(DateOnly asOfDate)
    {
        if (State != ConfigurationLifecycleState.Published)
        {
            return Result.Failure(ConfigurationErrors.InvalidLifecycleTransition);
        }

        if (asOfDate < EffectiveDate)
        {
            return Result.Failure(ConfigurationErrors.CannotActivateBeforeEffectiveDate);
        }

        State = ConfigurationLifecycleState.Active;
        return Result.Success();
    }

    internal Result Deprecate()
    {
        if (State != ConfigurationLifecycleState.Active)
        {
            return Result.Failure(ConfigurationErrors.InvalidLifecycleTransition);
        }

        State = ConfigurationLifecycleState.Deprecated;
        return Result.Success();
    }

    internal Result Archive()
    {
        if (State != ConfigurationLifecycleState.Deprecated)
        {
            return Result.Failure(ConfigurationErrors.InvalidLifecycleTransition);
        }

        State = ConfigurationLifecycleState.Archived;
        return Result.Success();
    }
}
