namespace Hris.Foundation.RulesEngine.Domain;

/// <summary>
/// The six states of rules-engine.md's own Rule Lifecycle diagram, in the exact
/// stated order: "Draft -&gt; Validated -&gt; Published -&gt; Active -&gt;
/// Deprecated -&gt; Archived." Distinct from, but structurally identical in shape
/// to, <c>ConfigurationLifecycleState</c> -- both frameworks independently specify
/// the same draft/publish/deprecate/archive workflow shape for versioned,
/// configurable business content, which is why <see cref="RuleVersion"/> mirrors
/// <c>ConfigurationVersion</c>'s own transition design.
/// </summary>
public enum RuleLifecycleState
{
    Draft = 0,
    Validated,
    Published,
    Active,
    Deprecated,
    Archived,
}
