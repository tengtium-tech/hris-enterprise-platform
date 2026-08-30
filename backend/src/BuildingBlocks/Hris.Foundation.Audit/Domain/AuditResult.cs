namespace Hris.Foundation.Audit.Domain;

/// <summary>
/// The outcome of the audited action, per audit-framework.md's Audit Record
/// Structure ("Result"). An audited action can itself have failed -- e.g. a login
/// failure, per the Security Audit examples ("Login Failure") -- and that failure is
/// exactly as auditable as a success; this is unrelated to whether *recording* the
/// audit entry succeeded.
/// </summary>
public enum AuditResult
{
    Success = 0,
    Failure,
}
