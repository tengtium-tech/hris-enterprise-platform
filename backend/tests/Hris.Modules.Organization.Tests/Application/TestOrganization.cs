using Hris.Modules.Organization.Domain;

namespace Hris.Modules.Organization.Tests.Application;

/// <summary>
/// Shared aggregate-construction helper for Application-layer handler tests, the
/// same role <c>TestData</c> plays for <c>Hris.Foundation.Integration.Tests</c>.
/// </summary>
internal static class TestOrganization
{
    public static readonly DateTimeOffset NowUtc = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static Hris.Modules.Organization.Domain.Organization Create() =>
        Hris.Modules.Organization.Domain.Organization.Create(
            new OrganizationId(Guid.NewGuid()), Guid.NewGuid(), "ABC Corporation", "CORP", null, null, NowUtc).Value;
}
