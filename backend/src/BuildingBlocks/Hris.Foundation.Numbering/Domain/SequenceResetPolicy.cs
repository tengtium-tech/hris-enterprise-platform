namespace Hris.Foundation.Numbering.Domain;

/// <summary>
/// Source: docs/03-foundation/numbering-framework.md, Sequence Management
/// ("Continuous Sequences... Annual Reset... Monthly Reset"). Organization-Based and
/// Department-Based sequences, also named in that same section, are not built here --
/// both require resolving live organizational-scope data this Sprint's own build has no
/// integration point for yet, the identical "real backing system doesn't exist in code
/// yet" deferral every other Sprint 3/4 framework's own DependencyInjection.cs already
/// states for at least one of its own nominally-unused dependencies.
/// </summary>
public enum SequenceResetPolicy
{
    Never = 0,
    Annual = 1,
    Monthly = 2,
}
