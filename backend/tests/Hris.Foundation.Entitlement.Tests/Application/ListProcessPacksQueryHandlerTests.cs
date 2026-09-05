using FluentAssertions;
using Hris.Foundation.Entitlement.Application.Queries;
using Hris.Foundation.Entitlement.Domain;
using Xunit;

namespace Hris.Foundation.Entitlement.Tests.Application;

public sealed class ListProcessPacksQueryHandlerTests
{
    private readonly ListProcessPacksQueryHandler _handler = new();

    [Fact]
    public async Task Handle_ReturnsAllTwentyOnePacks_WithEveryFieldPopulated()
    {
        var result = await _handler.Handle(new ListProcessPacksQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(21);

        var payroll = result.Value.Single(dto => dto.Code == nameof(ProcessPackCode.Payroll));
        payroll.DisplayName.Should().Be("Payroll");
        payroll.IsCore.Should().BeFalse();
        payroll.ConditionalDependencies.Should().BeEquivalentTo(new[]
        {
            nameof(ProcessPackCode.TimeAndAttendance),
            nameof(ProcessPackCode.Leave),
        });

        var employee = result.Value.Single(dto => dto.Code == nameof(ProcessPackCode.Employee));
        employee.DisplayName.Should().Be("Employee");
        employee.IsCore.Should().BeTrue();
        employee.ConditionalDependencies.Should().BeEmpty();
    }
}
