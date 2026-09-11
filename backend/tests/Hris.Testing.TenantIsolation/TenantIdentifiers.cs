namespace Hris.Testing.TenantIsolation;

/// <summary>
/// Two canonical tenant identifiers used by every tenant-isolation test.
/// Defined as constants so that structural seeding in the fixture and every
/// per-test assertion agree on the same Guids without re-creating them per run.
/// </summary>
public static class TenantIdentifiers
{
    public static Guid TenantAId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static Guid TenantBId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");
}
