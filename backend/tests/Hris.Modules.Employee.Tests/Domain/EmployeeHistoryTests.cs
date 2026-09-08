using FluentAssertions;
using Hris.Modules.Employee.Domain;
using Xunit;

namespace Hris.Modules.Employee.Tests.Domain;

public sealed class EmployeeHistoryTests
{
    [Fact]
    public void Record_CreatesHistoryWithEveryProperty()
    {
        var id = new EmployeeHistoryId(Guid.NewGuid());
        var tenantId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();
        var effectiveDate = DateOnly.FromDateTime(TestEmployee.NowUtc.UtcDateTime);

        var history = EmployeeHistory.Record(
            id, tenantId, employeeId, EmployeeHistoryCategory.LifecycleStage, "Hired", "Active", effectiveDate,
            "Onboarding completed", changedBy, TestEmployee.NowUtc);

        history.Id.Should().Be(id);
        history.TenantId.Should().Be(tenantId);
        history.EmployeeId.Should().Be(employeeId);
        history.Category.Should().Be(EmployeeHistoryCategory.LifecycleStage);
        history.PreviousValue.Should().Be("Hired");
        history.NewValue.Should().Be("Active");
        history.EffectiveDate.Should().Be(effectiveDate);
        history.BusinessReason.Should().Be("Onboarding completed");
        history.ChangedBy.Should().Be(changedBy);
        history.CreatedAtUtc.Should().Be(TestEmployee.NowUtc);
    }
}
