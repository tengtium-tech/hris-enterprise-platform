using FluentAssertions;
using Hris.Modules.Position.Domain;
using Xunit;

namespace Hris.Modules.Position.Tests.Domain;

public sealed class PositionTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_Succeeds_WithValidData()
    {
        var result = CreatePosition();

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Value.Should().Be("POS-000001");
        result.Value.Title.Value.Should().Be("Senior Software Engineer");
        result.Value.Status.Should().Be(PositionStatus.Draft);
        result.Value.VacancyStatus.Should().Be(VacancyStatus.Vacant);
        result.Value.DomainEvents.Should().ContainSingle(e => e is PositionCreated);
    }

    [Fact]
    public void Create_Fails_WhenNumberIsMissing()
    {
        var result = CreatePosition(number: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNumberRequired);
    }

    [Fact]
    public void Create_Fails_WhenTitleIsMissing()
    {
        var result = CreatePosition(title: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionTitleRequired);
    }

    [Fact]
    public void Create_Fails_WhenPositionTypeIsMissing()
    {
        var result = CreatePosition(positionType: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionTypeRequired);
    }

    [Fact]
    public void Create_Fails_WhenReportingPositionIsSelf()
    {
        var id = new PositionId(Guid.NewGuid());

        var result = Hris.Modules.Position.Domain.Position.Create(
            id, Guid.NewGuid(), "POS-000001", "Title", "Type", Guid.NewGuid(), null, null, null, null, null, null,
            null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), id.Value, 1, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.SelfReportingProhibited);
    }

    [Fact]
    public void Create_Fails_WhenAuthorizedHeadcountIsNegative()
    {
        var result = CreatePosition(authorizedHeadcount: -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.AuthorizedHeadcountNegative);
    }

    [Fact]
    public void Update_Succeeds_WhenNotArchived()
    {
        var position = CreatePosition().Value;

        var result = position.Update(
            "New Title", "New Type", Guid.NewGuid(), null, null, null, null, null, null, null, null, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), _now);

        result.IsSuccess.Should().BeTrue();
        position.Title.Value.Should().Be("New Title");
    }

    [Fact]
    public void Update_Fails_WhenArchived()
    {
        var position = CreateActivePosition();
        position.Archive(_now);

        var result = position.Update(
            "New Title", "New Type", Guid.NewGuid(), null, null, null, null, null, null, null, null, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void Activate_Succeeds_FromDraft()
    {
        var position = CreatePosition().Value;

        var result = position.Activate(_now);

        result.IsSuccess.Should().BeTrue();
        position.Status.Should().Be(PositionStatus.Active);
    }

    [Fact]
    public void Activate_Succeeds_FromInactive()
    {
        var position = CreateActivePosition();
        position.Deactivate(_now);

        var result = position.Activate(_now);

        result.IsSuccess.Should().BeTrue();
        position.Status.Should().Be(PositionStatus.Active);
    }

    [Fact]
    public void Activate_Fails_WhenAlreadyActive()
    {
        var position = CreateActivePosition();

        var result = position.Activate(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionAlreadyActive);
    }

    [Fact]
    public void Activate_Fails_WhenArchived()
    {
        var position = CreateActivePosition();
        position.Archive(_now);

        var result = position.Activate(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void Deactivate_Succeeds_WhenActive()
    {
        var position = CreateActivePosition();

        var result = position.Deactivate(_now);

        result.IsSuccess.Should().BeTrue();
        position.Status.Should().Be(PositionStatus.Inactive);
    }

    [Fact]
    public void Deactivate_Fails_WhenDraft()
    {
        var position = CreatePosition().Value;

        var result = position.Deactivate(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotActive);
    }

    [Fact]
    public void Deactivate_Fails_WhenArchived()
    {
        var position = CreateActivePosition();
        position.Archive(_now);

        var result = position.Deactivate(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void Archive_Succeeds_WhenActive()
    {
        var position = CreateActivePosition();

        var result = position.Archive(_now);

        result.IsSuccess.Should().BeTrue();
        position.Status.Should().Be(PositionStatus.Archived);
    }

    [Fact]
    public void Archive_Succeeds_WhenInactive()
    {
        var position = CreateActivePosition();
        position.Deactivate(_now);

        var result = position.Archive(_now);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Archive_Fails_WhenDraft()
    {
        var position = CreatePosition().Value;

        var result = position.Archive(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionCannotArchiveDraft);
    }

    [Fact]
    public void Archive_Fails_WhenAlreadyArchived()
    {
        var position = CreateActivePosition();
        position.Archive(_now);

        var result = position.Archive(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.AlreadyArchived);
    }

    [Fact]
    public void AssignReportingPosition_Succeeds_WhenValid()
    {
        var position = CreatePosition().Value;
        var reportingPositionId = Guid.NewGuid();

        var result = position.AssignReportingPosition(reportingPositionId, true, true, false, _now);

        result.IsSuccess.Should().BeTrue();
        position.ReportingPositionId.Should().Be(reportingPositionId);
        position.DomainEvents.Should().Contain(e => e is ReportingPositionAssigned);
    }

    [Fact]
    public void AssignReportingPosition_RaisesChangedEvent_WhenAlreadyAssigned()
    {
        var position = CreatePosition().Value;
        position.AssignReportingPosition(Guid.NewGuid(), true, true, false, _now);

        var result = position.AssignReportingPosition(Guid.NewGuid(), true, true, false, _now);

        result.IsSuccess.Should().BeTrue();
        position.DomainEvents.Should().Contain(e => e is ReportingPositionChanged);
    }

    [Fact]
    public void AssignReportingPosition_Fails_WhenSelfReport()
    {
        var position = CreatePosition().Value;

        var result = position.AssignReportingPosition(position.Id.Value, true, true, false, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.SelfReportingProhibited);
    }

    [Fact]
    public void AssignReportingPosition_Fails_WhenNotExists()
    {
        var position = CreatePosition().Value;

        var result = position.AssignReportingPosition(Guid.NewGuid(), false, false, false, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ReportingPositionNotFound);
    }

    [Fact]
    public void AssignReportingPosition_Fails_WhenNotActive()
    {
        var position = CreatePosition().Value;

        var result = position.AssignReportingPosition(Guid.NewGuid(), true, false, false, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ReportingPositionMustBeActive);
    }

    [Fact]
    public void AssignReportingPosition_Fails_WhenWouldCreateCycle()
    {
        var position = CreatePosition().Value;

        var result = position.AssignReportingPosition(Guid.NewGuid(), true, true, true, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.CircularReportingProhibited);
    }

    [Fact]
    public void AssignReportingPosition_Fails_WhenArchived()
    {
        var position = CreateActivePosition();
        position.Archive(_now);

        var result = position.AssignReportingPosition(Guid.NewGuid(), true, true, false, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void RemoveReportingPosition_Succeeds_WhenAssigned()
    {
        var position = CreatePosition().Value;
        position.AssignReportingPosition(Guid.NewGuid(), true, true, false, _now);

        var result = position.RemoveReportingPosition(_now);

        result.IsSuccess.Should().BeTrue();
        position.ReportingPositionId.Should().BeNull();
    }

    [Fact]
    public void RemoveReportingPosition_Fails_WhenNoneAssigned()
    {
        var position = CreatePosition().Value;

        var result = position.RemoveReportingPosition(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.NoReportingPositionToRemove);
    }

    [Fact]
    public void RemoveReportingPosition_Fails_WhenArchived()
    {
        var position = CreateActivePosition();
        position.Archive(_now);

        var result = position.RemoveReportingPosition(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void UpdateAuthorizedHeadcount_Succeeds()
    {
        var position = CreatePosition().Value;

        var result = position.UpdateAuthorizedHeadcount(5, _now);

        result.IsSuccess.Should().BeTrue();
        position.AuthorizedHeadcount.Value.Should().Be(5);
    }

    [Fact]
    public void UpdateAuthorizedHeadcount_Fails_WhenNegative()
    {
        var position = CreatePosition().Value;

        var result = position.UpdateAuthorizedHeadcount(-1, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.AuthorizedHeadcountNegative);
    }

    [Fact]
    public void UpdateAuthorizedHeadcount_Fails_WhenArchived()
    {
        var position = CreateActivePosition();
        position.Archive(_now);

        var result = position.UpdateAuthorizedHeadcount(5, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void MarkFilled_Succeeds_WhenVacant()
    {
        var position = CreatePosition().Value;

        var result = position.MarkFilled(_now);

        result.IsSuccess.Should().BeTrue();
        position.VacancyStatus.Should().Be(VacancyStatus.Filled);
    }

    [Fact]
    public void MarkFilled_Fails_WhenAlreadyFilled()
    {
        var position = CreatePosition().Value;
        position.MarkFilled(_now);

        var result = position.MarkFilled(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionAlreadyFilled);
    }

    [Fact]
    public void MarkVacant_Succeeds_WhenFilled()
    {
        var position = CreatePosition().Value;
        position.MarkFilled(_now);

        var result = position.MarkVacant(_now);

        result.IsSuccess.Should().BeTrue();
        position.VacancyStatus.Should().Be(VacancyStatus.Vacant);
    }

    [Fact]
    public void MarkVacant_Fails_WhenAlreadyVacant()
    {
        var position = CreatePosition().Value;

        var result = position.MarkVacant(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionAlreadyVacant);
    }

    [Fact]
    public void MarkFilled_Fails_WhenArchived()
    {
        var position = CreateActivePosition();
        position.Archive(_now);

        var result = position.MarkFilled(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void MarkVacant_Fails_WhenArchived()
    {
        var position = CreateActivePosition();
        position.MarkFilled(_now);
        position.Deactivate(_now);
        position.Archive(_now);

        var result = position.MarkVacant(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    private static Hris.SharedKernel.Result<Hris.Modules.Position.Domain.Position> CreatePosition(
        string? number = "POS-000001", string? title = "Senior Software Engineer", string? positionType = "Individual Contributor",
        int authorizedHeadcount = 1) => Hris.Modules.Position.Domain.Position.Create(
            new PositionId(Guid.NewGuid()), Guid.NewGuid(), number, title, positionType, Guid.NewGuid(), null, null,
            null, null, null, null, null, null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            authorizedHeadcount, _now);

    private static Hris.Modules.Position.Domain.Position CreateActivePosition()
    {
        var position = CreatePosition().Value;
        position.Activate(_now);
        return position;
    }
}
