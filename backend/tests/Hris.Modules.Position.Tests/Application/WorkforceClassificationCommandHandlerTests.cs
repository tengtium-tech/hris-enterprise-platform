using FluentAssertions;
using Hris.Modules.Position.Application.Commands;
using Hris.Modules.Position.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Position.Tests.Application;

public sealed class CreateJobFamilyCommandHandlerTests
{
    private readonly IJobFamilyRepository _repository = Substitute.For<IJobFamilyRepository>();
    private readonly CreateJobFamilyCommandHandler _handler;

    public CreateJobFamilyCommandHandlerTests()
    {
        _handler = new CreateJobFamilyCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCodeAndNameAreUnique()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "IT", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsWithNameAsync(tenantId, "Information Technology", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreateJobFamilyCommand(tenantId, "IT", "Information Technology", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<JobFamily>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "IT", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateJobFamilyCommand(tenantId, "IT", "Information Technology", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.DuplicateJobFamilyCode);
    }

    [Fact]
    public async Task Handle_Fails_WhenNameAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "IT", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsWithNameAsync(tenantId, "Information Technology", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateJobFamilyCommand(tenantId, "IT", "Information Technology", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.DuplicateJobFamilyName);
    }
}

public sealed class UpdateJobFamilyCommandHandlerTests
{
    private readonly IJobFamilyRepository _repository = Substitute.For<IJobFamilyRepository>();
    private readonly UpdateJobFamilyCommandHandler _handler;

    public UpdateJobFamilyCommandHandlerTests()
    {
        _handler = new UpdateJobFamilyCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenJobFamilyExists()
    {
        var tenantId = Guid.NewGuid();
        var jobFamily = TestPosition.CreateJobFamily(tenantId);
        _repository.GetByIdAsync(jobFamily.Id, Arg.Any<CancellationToken>()).Returns(jobFamily);
        _repository.ExistsWithNameAsync(tenantId, "New Name", jobFamily.Id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new UpdateJobFamilyCommand(jobFamily.Id.Value, tenantId, "New Name", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jobFamily.Name.Value.Should().Be("New Name");
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<JobFamilyId>(), Arg.Any<CancellationToken>()).Returns((JobFamily?)null);

        var result = await _handler.Handle(
            new UpdateJobFamilyCommand(Guid.NewGuid(), Guid.NewGuid(), "New Name", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobFamilyNotFound);
    }
}

public sealed class JobFamilyLifecycleCommandHandlerTests
{
    private readonly IJobFamilyRepository _repository = Substitute.For<IJobFamilyRepository>();

    [Fact]
    public async Task Activate_Succeeds_WhenInactive()
    {
        var tenantId = Guid.NewGuid();
        var jobFamily = TestPosition.CreateJobFamily(tenantId);
        jobFamily.Deactivate(TestPosition.NowUtc);
        _repository.GetByIdAsync(jobFamily.Id, Arg.Any<CancellationToken>()).Returns(jobFamily);
        var handler = new ActivateJobFamilyCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));

        var result = await handler.Handle(new ActivateJobFamilyCommand(jobFamily.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Deactivate_Succeeds_WhenActive()
    {
        var tenantId = Guid.NewGuid();
        var jobFamily = TestPosition.CreateJobFamily(tenantId);
        _repository.GetByIdAsync(jobFamily.Id, Arg.Any<CancellationToken>()).Returns(jobFamily);
        var handler = new DeactivateJobFamilyCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));

        var result = await handler.Handle(new DeactivateJobFamilyCommand(jobFamily.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Archive_Succeeds_WhenActive()
    {
        var tenantId = Guid.NewGuid();
        var jobFamily = TestPosition.CreateJobFamily(tenantId);
        _repository.GetByIdAsync(jobFamily.Id, Arg.Any<CancellationToken>()).Returns(jobFamily);
        var handler = new ArchiveJobFamilyCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));

        var result = await handler.Handle(new ArchiveJobFamilyCommand(jobFamily.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Archive_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<JobFamilyId>(), Arg.Any<CancellationToken>()).Returns((JobFamily?)null);
        var handler = new ArchiveJobFamilyCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));

        var result = await handler.Handle(new ArchiveJobFamilyCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobFamilyNotFound);
    }
}

public sealed class CreateJobClassificationCommandHandlerTests
{
    private readonly IJobClassificationRepository _repository = Substitute.For<IJobClassificationRepository>();
    private readonly CreateJobClassificationCommandHandler _handler;

    public CreateJobClassificationCommandHandlerTests()
    {
        _handler = new CreateJobClassificationCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCodeAndNameAreUnique()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "PROF", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsWithNameAsync(tenantId, "Professional", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreateJobClassificationCommand(tenantId, "PROF", "Professional", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<JobClassification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenCodeAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "PROF", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateJobClassificationCommand(tenantId, "PROF", "Professional", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.DuplicateJobClassificationCode);
    }
}

public sealed class JobClassificationLifecycleCommandHandlerTests
{
    private readonly IJobClassificationRepository _repository = Substitute.For<IJobClassificationRepository>();

    [Fact]
    public async Task Update_Succeeds_WhenFound()
    {
        var tenantId = Guid.NewGuid();
        var jobClassification = TestPosition.CreateJobClassification(tenantId);
        _repository.GetByIdAsync(jobClassification.Id, Arg.Any<CancellationToken>()).Returns(jobClassification);
        var handler = new UpdateJobClassificationCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));

        var result = await handler.Handle(
            new UpdateJobClassificationCommand(jobClassification.Id.Value, tenantId, "New Name", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Activate_Then_Deactivate_Then_Archive_AllSucceed()
    {
        var tenantId = Guid.NewGuid();
        var jobClassification = TestPosition.CreateJobClassification(tenantId);
        _repository.GetByIdAsync(jobClassification.Id, Arg.Any<CancellationToken>()).Returns(jobClassification);
        var timeProvider = new FakeTimeProvider(TestPosition.NowUtc);

        (await new DeactivateJobClassificationCommandHandler(_repository, timeProvider)
            .Handle(new DeactivateJobClassificationCommand(jobClassification.Id.Value, tenantId), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await new ActivateJobClassificationCommandHandler(_repository, timeProvider)
            .Handle(new ActivateJobClassificationCommand(jobClassification.Id.Value, tenantId), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await new ArchiveJobClassificationCommandHandler(_repository, timeProvider)
            .Handle(new ArchiveJobClassificationCommand(jobClassification.Id.Value, tenantId), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
    }
}

public sealed class CreateJobGradeCommandHandlerTests
{
    private readonly IJobGradeRepository _repository = Substitute.For<IJobGradeRepository>();
    private readonly CreateJobGradeCommandHandler _handler;

    public CreateJobGradeCommandHandlerTests()
    {
        _handler = new CreateJobGradeCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenCodeAndNameAreUnique()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "G08", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsWithNameAsync(tenantId, "Grade 8", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreateJobGradeCommand(tenantId, "G08", "Grade 8", null, 8), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<JobGrade>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNameAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithCodeAsync(tenantId, "G08", null, Arg.Any<CancellationToken>()).Returns(false);
        _repository.ExistsWithNameAsync(tenantId, "Grade 8", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateJobGradeCommand(tenantId, "G08", "Grade 8", null, 8), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.DuplicateJobGradeName);
    }
}

public sealed class JobGradeLifecycleCommandHandlerTests
{
    private readonly IJobGradeRepository _repository = Substitute.For<IJobGradeRepository>();

    [Fact]
    public async Task Update_Succeeds_WhenFound()
    {
        var tenantId = Guid.NewGuid();
        var jobGrade = TestPosition.CreateJobGrade(tenantId);
        _repository.GetByIdAsync(jobGrade.Id, Arg.Any<CancellationToken>()).Returns(jobGrade);
        var handler = new UpdateJobGradeCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));

        var result = await handler.Handle(
            new UpdateJobGradeCommand(jobGrade.Id.Value, tenantId, "Grade 9", null, 9), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        jobGrade.OrganizationalLevel.Should().Be(9);
    }

    [Fact]
    public async Task Update_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<JobGradeId>(), Arg.Any<CancellationToken>()).Returns((JobGrade?)null);
        var handler = new UpdateJobGradeCommandHandler(_repository, new FakeTimeProvider(TestPosition.NowUtc));

        var result = await handler.Handle(
            new UpdateJobGradeCommand(Guid.NewGuid(), Guid.NewGuid(), "Grade 9", null, 9), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobGradeNotFound);
    }

    [Fact]
    public async Task Activate_Deactivate_Archive_AllSucceed()
    {
        var tenantId = Guid.NewGuid();
        var jobGrade = TestPosition.CreateJobGrade(tenantId);
        _repository.GetByIdAsync(jobGrade.Id, Arg.Any<CancellationToken>()).Returns(jobGrade);
        var timeProvider = new FakeTimeProvider(TestPosition.NowUtc);

        (await new DeactivateJobGradeCommandHandler(_repository, timeProvider)
            .Handle(new DeactivateJobGradeCommand(jobGrade.Id.Value, tenantId), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await new ActivateJobGradeCommandHandler(_repository, timeProvider)
            .Handle(new ActivateJobGradeCommand(jobGrade.Id.Value, tenantId), CancellationToken.None))
            .IsSuccess.Should().BeTrue();

        (await new ArchiveJobGradeCommandHandler(_repository, timeProvider)
            .Handle(new ArchiveJobGradeCommand(jobGrade.Id.Value, tenantId), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
    }
}
