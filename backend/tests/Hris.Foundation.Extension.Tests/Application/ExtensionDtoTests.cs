using FluentAssertions;
using Hris.Foundation.Extension.Application.Dtos;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

/// <summary>
/// Confirms these DTOs behave as proper records (value equality, a real
/// <c>ToString</c>) -- FluentAssertions' <c>BeEquivalentTo</c>, used throughout the
/// handler tests, never exercises a record's own generated members, the identical gap
/// <c>TenantDtoTests</c> already closes for its own sibling framework.
/// </summary>
public sealed class ExtensionDtoTests
{
    [Fact]
    public void ExtensionPointDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new ExtensionPointDto(
            Guid.NewGuid(), "employee.before-save", "Before Employee Save", "desc", "BusinessLogic", "Employee",
            ["Before", "After"], "Draft", 1);
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(ExtensionPointDto));
    }

    [Fact]
    public void HookDto_HasValueEquality_AndAUsefulToString()
    {
        var original = new HookDto(Guid.NewGuid(), Guid.NewGuid(), "Before", "Handler.Reference", "Employee", "Active");
        var clone = original with { };

        clone.Should().Be(original);
        original.ToString().Should().Contain(nameof(HookDto));
    }
}
