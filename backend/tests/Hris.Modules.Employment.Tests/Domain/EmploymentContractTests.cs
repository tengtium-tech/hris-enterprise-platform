using FluentAssertions;
using Hris.Modules.Employment.Domain;
using Xunit;

namespace Hris.Modules.Employment.Tests.Domain;

public sealed class EmploymentContractTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly DateOnly _startDate = DateOnly.FromDateTime(TestEmployment.NowUtc.UtcDateTime);

    [Fact]
    public void Create_IndefiniteContract_Succeeds()
    {
        var result = EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "Regular", _startDate, null, false,
            null, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.LifecycleStage.Should().Be(ContractLifecycleStage.Draft);
        result.Value.Period.EndDate.Should().BeNull();
    }

    [Fact]
    public void Create_FixedTermWithoutEndDate_Fails()
    {
        var result = EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "Contractual", _startDate, null, true,
            null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.FixedTermContractRequiresEndDate);
    }

    [Fact]
    public void Create_EndDateBeforeStartDate_Fails()
    {
        var result = EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "Contractual", _startDate,
            _startDate.AddDays(-1), true, null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractPeriodEndBeforeStart);
    }

    [Fact]
    public void Approve_FromDraft_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.Approve(TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Approved);
    }

    [Fact]
    public void Approve_WhenNotDraft_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);

        var result = contract.Approve(TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractNotDraft);
    }

    [Fact]
    public void MakeEffective_FromApproved_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);

        var result = contract.MakeEffective(TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Effective);
    }

    [Fact]
    public void MakeEffective_WhenDraft_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.MakeEffective(TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractNotApproved);
    }

    [Fact]
    public void Renew_WhenEffective_UpdatesPeriodAndRecordsHistory()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Renew(_startDate.AddYears(1), _startDate.AddYears(2), "Approved by HR", TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.Renewals.Should().HaveCount(1);
        contract.Period.StartDate.Should().Be(_startDate.AddYears(1));
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Effective);
    }

    [Fact]
    public void Renew_WhenClosed_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);
        contract.Close(TestEmployment.NowUtc);

        var result = contract.Renew(_startDate.AddYears(1), null, null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractCannotRenewAfterClosed);
    }

    [Fact]
    public void Extend_WhenEffective_UpdatesEndDateAndRecordsHistory()
    {
        var contract = EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "Contractual", _startDate,
            _startDate.AddMonths(6), true, null, TestEmployment.NowUtc).Value;
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Extend(_startDate.AddMonths(9), "Project extended", TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.Extensions.Should().HaveCount(1);
        contract.Period.EndDate.Should().Be(_startDate.AddMonths(9));
    }

    [Fact]
    public void Extend_WhenDraft_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.Extend(_startDate.AddMonths(6), null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractNotEffective);
    }

    [Fact]
    public void Expire_WhenEffective_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Expire(TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Expired);
    }

    [Fact]
    public void Supersede_WhenEffective_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Supersede(TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Superseded);
    }

    [Fact]
    public void Close_WhenEffective_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Close(TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Closed);
    }

    [Fact]
    public void Cancel_WhenDraft_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.Cancel(TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Cancelled);
    }

    [Fact]
    public void Cancel_WhenEffective_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Cancel(TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractNotDraftOrApproved);
    }

    [Fact]
    public void AddDocument_WithValidData_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.AddDocument("SignedContract", "documents/abc123", 1, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.Documents.Should().HaveCount(1);
    }

    [Fact]
    public void AddDocument_WithoutStorageReference_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.AddDocument("SignedContract", null, 1, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_WithInvalidContractType_Fails()
    {
        var result = EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), null, _startDate, null, false, null,
            TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractTypeRequired);
    }

    [Fact]
    public void Renew_WithEndDateBeforeStartDate_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Renew(_startDate.AddYears(1), _startDate, null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractPeriodEndBeforeStart);
    }

    [Fact]
    public void Extend_WithNewEndDateBeforeCurrentEnd_Fails()
    {
        var contract = EmploymentContract.Create(
            new EmploymentContractId(Guid.NewGuid()), _tenantId, Guid.NewGuid(), "Contractual", _startDate,
            _startDate.AddMonths(6), true, null, TestEmployment.NowUtc).Value;
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Extend(_startDate.AddMonths(3), null, TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractPeriodEndBeforeStart);
    }

    [Fact]
    public void Extend_OnOpenEndedContract_WithNullReason_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Extend(_startDate.AddYears(1), null, TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.Extensions[0].Reason.Should().BeEmpty();
        contract.Period.EndDate.Should().Be(_startDate.AddYears(1));
    }

    [Fact]
    public void Extend_OnOpenEndedContract_WithNewEndDateBeforeStart_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);
        contract.MakeEffective(TestEmployment.NowUtc);

        var result = contract.Extend(_startDate.AddDays(-1), "Backdated", TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractPeriodEndBeforeStart);
    }

    [Fact]
    public void Expire_WhenDraft_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.Expire(TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractNotEffective);
    }

    [Fact]
    public void Supersede_WhenDraft_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.Supersede(TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractNotEffective);
    }

    [Fact]
    public void Supersede_WhenApproved_Succeeds()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());
        contract.Approve(TestEmployment.NowUtc);

        var result = contract.Supersede(TestEmployment.NowUtc);

        result.IsSuccess.Should().BeTrue();
        contract.LifecycleStage.Should().Be(ContractLifecycleStage.Superseded);
    }

    [Fact]
    public void Close_WhenDraft_Fails()
    {
        var contract = TestEmployment.CreateContract(_tenantId, Guid.NewGuid());

        var result = contract.Close(TestEmployment.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(EmploymentErrors.ContractNotEffective);
    }
}
