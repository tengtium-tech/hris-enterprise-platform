namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// The seven levels of configuration-framework.md's Configuration Hierarchy, from
/// most general to most specific: "Global -&gt; Tenant -&gt; Company -&gt; Legal
/// Entity -&gt; Business Unit -&gt; Department -&gt; Individual Override ... More
/// specific configuration overrides higher-level defaults." Ordinal order is
/// meaningful: <see cref="ConfigurationHierarchyResolver"/> compares levels by this
/// declared order to find the most specific applicable value, so a member must never
/// be reordered or inserted out of sequence.
///
/// A Simple Enumeration per docs/02-architecture/04-domain-driven-design/enumeration-pattern.md
/// ("Values rarely change ... The value set is inherently fixed") -- the override
/// precedence behavior lives in <see cref="ConfigurationHierarchyResolver"/>, not on
/// this type itself, since it is a domain service concern spanning many
/// <see cref="ConfigurationSetting"/> aggregate instances rather than a fact about
/// one level.
/// </summary>
public enum ConfigurationScopeLevel
{
    Global = 0,
    Tenant = 1,
    Company = 2,
    LegalEntity = 3,
    BusinessUnit = 4,
    Department = 5,
    IndividualOverride = 6,
}
