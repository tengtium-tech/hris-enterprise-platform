using FluentAssertions;
using Hris.Modules.Position.Domain;
using Xunit;

namespace Hris.Modules.Position.Tests.Domain;

/// <summary>
/// Covers <see cref="JobFamily"/>, <see cref="JobClassification"/>, and
/// <see cref="JobGrade"/> together: all three share the identical
/// <see cref="WorkforceClassificationStatus"/> lifecycle and near-identical
/// Code/Name/Description shape (job-families.md, job-classifications.md,
/// job-grades.md).
/// </summary>
public sealed class WorkforceClassificationTests
{
    private static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void JobFamily_Create_Succeeds_WithValidCodeAndName()
    {
        var result = JobFamily.Create(new JobFamilyId(Guid.NewGuid()), Guid.NewGuid(), "IT", "Information Technology", null, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Value.Should().Be("IT");
        result.Value.Status.Should().Be(WorkforceClassificationStatus.Active);
        result.Value.DomainEvents.Should().ContainSingle(e => e is JobFamilyCreated);
    }

    [Fact]
    public void JobFamily_Create_Fails_WhenCodeIsMissing()
    {
        var result = JobFamily.Create(new JobFamilyId(Guid.NewGuid()), Guid.NewGuid(), null, "Information Technology", null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobFamilyCodeRequired);
    }

    [Fact]
    public void JobFamily_Create_Fails_WhenNameIsMissing()
    {
        var result = JobFamily.Create(new JobFamilyId(Guid.NewGuid()), Guid.NewGuid(), "IT", null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobFamilyNameRequired);
    }

    [Fact]
    public void JobFamily_Update_Fails_WhenArchived()
    {
        var jobFamily = CreateJobFamily();
        jobFamily.Archive(_now);

        var result = jobFamily.Update("New Name", null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void JobFamily_Deactivate_Succeeds_WhenActive()
    {
        var jobFamily = CreateJobFamily();

        var result = jobFamily.Deactivate(_now);

        result.IsSuccess.Should().BeTrue();
        jobFamily.Status.Should().Be(WorkforceClassificationStatus.Inactive);
    }

    [Fact]
    public void JobFamily_Deactivate_Fails_WhenAlreadyInactive()
    {
        var jobFamily = CreateJobFamily();
        jobFamily.Deactivate(_now);

        var result = jobFamily.Deactivate(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.WorkforceClassificationAlreadyInactive);
    }

    [Fact]
    public void JobFamily_Deactivate_Fails_WhenArchived()
    {
        var jobFamily = CreateJobFamily();
        jobFamily.Archive(_now);

        var result = jobFamily.Deactivate(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void JobFamily_Activate_Succeeds_WhenInactive()
    {
        var jobFamily = CreateJobFamily();
        jobFamily.Deactivate(_now);

        var result = jobFamily.Activate(_now);

        result.IsSuccess.Should().BeTrue();
        jobFamily.Status.Should().Be(WorkforceClassificationStatus.Active);
    }

    [Fact]
    public void JobFamily_Activate_Fails_WhenAlreadyActive()
    {
        var jobFamily = CreateJobFamily();

        var result = jobFamily.Activate(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.WorkforceClassificationAlreadyActive);
    }

    [Fact]
    public void JobFamily_Activate_Fails_WhenArchived()
    {
        var jobFamily = CreateJobFamily();
        jobFamily.Archive(_now);

        var result = jobFamily.Activate(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void JobFamily_Archive_Fails_WhenAlreadyArchived()
    {
        var jobFamily = CreateJobFamily();
        jobFamily.Archive(_now);

        var result = jobFamily.Archive(_now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.AlreadyArchived);
    }

    [Fact]
    public void JobClassification_Create_Succeeds_WithValidCodeAndName()
    {
        var result = JobClassification.Create(
            new JobClassificationId(Guid.NewGuid()), Guid.NewGuid(), "PROF", "Professional", "desc", _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Value.Should().Be("PROF");
        result.Value.DomainEvents.Should().ContainSingle(e => e is JobClassificationCreated);
    }

    [Fact]
    public void JobClassification_Create_Fails_WhenCodeIsMissing()
    {
        var result = JobClassification.Create(new JobClassificationId(Guid.NewGuid()), Guid.NewGuid(), null, "Professional", null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobClassificationCodeRequired);
    }

    [Fact]
    public void JobClassification_Create_Fails_WhenNameIsMissing()
    {
        var result = JobClassification.Create(new JobClassificationId(Guid.NewGuid()), Guid.NewGuid(), "PROF", null, null, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobClassificationNameRequired);
    }

    [Fact]
    public void JobClassification_Update_Succeeds_WhenActive()
    {
        var jobClassification = CreateJobClassification();

        var result = jobClassification.Update("New Name", "New Description", _now);

        result.IsSuccess.Should().BeTrue();
        jobClassification.Name.Value.Should().Be("New Name");
    }

    [Fact]
    public void JobClassification_Deactivate_Then_Activate_RoundTrips()
    {
        var jobClassification = CreateJobClassification();

        jobClassification.Deactivate(_now).IsSuccess.Should().BeTrue();
        jobClassification.Activate(_now).IsSuccess.Should().BeTrue();

        jobClassification.Status.Should().Be(WorkforceClassificationStatus.Active);
    }

    [Fact]
    public void JobClassification_Archive_Succeeds_WhenActive()
    {
        var jobClassification = CreateJobClassification();

        var result = jobClassification.Archive(_now);

        result.IsSuccess.Should().BeTrue();
        jobClassification.Status.Should().Be(WorkforceClassificationStatus.Archived);
    }

    [Fact]
    public void JobGrade_Create_Succeeds_WithOrganizationalLevel()
    {
        var result = JobGrade.Create(new JobGradeId(Guid.NewGuid()), Guid.NewGuid(), "G08", "Grade 8", null, 8, _now);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrganizationalLevel.Should().Be(8);
        result.Value.DomainEvents.Should().ContainSingle(e => e is JobGradeCreated);
    }

    [Fact]
    public void JobGrade_Create_Fails_WhenCodeIsMissing()
    {
        var result = JobGrade.Create(new JobGradeId(Guid.NewGuid()), Guid.NewGuid(), null, "Grade 8", null, 8, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobGradeCodeRequired);
    }

    [Fact]
    public void JobGrade_Create_Fails_WhenNameIsMissing()
    {
        var result = JobGrade.Create(new JobGradeId(Guid.NewGuid()), Guid.NewGuid(), "G08", null, null, 8, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobGradeNameRequired);
    }

    [Fact]
    public void JobGrade_Update_Succeeds_AndChangesOrganizationalLevel()
    {
        var jobGrade = CreateJobGrade();

        var result = jobGrade.Update("Grade 9", null, 9, _now);

        result.IsSuccess.Should().BeTrue();
        jobGrade.OrganizationalLevel.Should().Be(9);
    }

    [Fact]
    public void JobGrade_Update_Fails_WhenArchived()
    {
        var jobGrade = CreateJobGrade();
        jobGrade.Archive(_now);

        var result = jobGrade.Update("Grade 9", null, 9, _now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.ArchivedCannotBeModified);
    }

    [Fact]
    public void JobGrade_Archive_Succeeds_WhenInactive()
    {
        var jobGrade = CreateJobGrade();
        jobGrade.Deactivate(_now);

        var result = jobGrade.Archive(_now);

        result.IsSuccess.Should().BeTrue();
    }

    private static JobFamily CreateJobFamily() =>
        JobFamily.Create(new JobFamilyId(Guid.NewGuid()), Guid.NewGuid(), "IT", "Information Technology", null, _now).Value;

    private static JobClassification CreateJobClassification() =>
        JobClassification.Create(new JobClassificationId(Guid.NewGuid()), Guid.NewGuid(), "PROF", "Professional", null, _now).Value;

    private static JobGrade CreateJobGrade() =>
        JobGrade.Create(new JobGradeId(Guid.NewGuid()), Guid.NewGuid(), "G08", "Grade 8", null, 8, _now).Value;
}
