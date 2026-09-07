namespace Hris.SharedKernel;

/// <summary>
/// Marks a property as carrying sensitive personal data, credentials, or payroll
/// values that must never appear in a log entry -- docs/08-devops/monitoring-and-alerting.md's
/// own Structured Logging section (NFR-OB-001): "a Serilog destructuring policy (or
/// equivalent enricher) applied to types known to carry sensitive data... redacts or
/// omits the sensitive fields before the log event is emitted, so that a developer
/// who logs an entire object for debugging convenience does not thereby leak the data
/// NFR-OB-001 prohibits. A code-review checklist item alone would rely on every
/// reviewer remembering this on every change; a destructuring policy makes the
/// omission the default."
///
/// Declared in the shared kernel, not in <c>Hris.Api</c>, so that any future module's
/// own Domain or DTO type (an <c>Employee</c>, a <c>PayrollResult</c>, a <c>Money</c>
/// value at the individual-employee level -- this document's own named examples) can
/// mark its own sensitive properties without that module taking a dependency on the
/// Presentation-layer project that actually enforces the redaction
/// (<c>Hris.Api</c>'s own <c>SensitiveDataDestructuringPolicy</c>). No concrete type in
/// this Sprint's own build actually carries this attribute yet -- Employee and
/// PayrollResult are Phase 2/3 modules, not built here -- the identical "a real
/// mechanism with no concrete consumer yet, not a gap" pattern this codebase already
/// applies to every framework's own unwired Upstream Dependency.
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class SensitiveDataAttribute : Attribute;
