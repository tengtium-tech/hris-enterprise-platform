namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// The seven states of configuration-framework.md's Configuration Lifecycle, in the
/// exact stated order: "Draft -&gt; Validated -&gt; Approved -&gt; Published -&gt;
/// Active -&gt; Deprecated -&gt; Archived." <see cref="ConfigurationVersion"/>'s own
/// transition methods are the only place this field changes, and each rejects a
/// transition that is not the immediate next state, per
/// docs/02-architecture/04-domain-driven-design/invariants.md's State Transition
/// Rules ("Illegal transitions must be rejected").
///
/// A version's stored state reflects administrative workflow progress, not
/// necessarily what value is in force on a given date --
/// <see cref="ConfigurationSetting.GetValueAsOf"/> answers that question
/// independently, from effective/expiration dates alone, so a historical query's
/// answer never depends on whether an operator has since gotten around to calling
/// <c>Activate()</c>. See that method's own remarks.
/// </summary>
public enum ConfigurationLifecycleState
{
    Draft = 0,
    Validated,
    Approved,
    Published,
    Active,
    Deprecated,
    Archived,
}
