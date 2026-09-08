using FluentAssertions;
using Hris.Modules.Position.Application.Queries;
using Hris.Modules.Position.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Position.Tests.Application;

public sealed class GetPositionQueryHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly GetPositionQueryHandler _handler;

    public GetPositionQueryHandlerTests()
    {
        _handler = new GetPositionQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenFound()
    {
        var tenantId = Guid.NewGuid();
        var position = TestPosition.Create(tenantId);
        _repository.GetByIdAsync(position.Id, Arg.Any<CancellationToken>()).Returns(position);

        var result = await _handler.Handle(new GetPositionQuery(position.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Number.Should().Be("POS-000001");
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<PositionId>(), Arg.Any<CancellationToken>()).Returns((Hris.Modules.Position.Domain.Position?)null);

        var result = await _handler.Handle(new GetPositionQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.PositionNotFound);
    }
}

public sealed class ListPositionsQueryHandlerTests
{
    private readonly IPositionRepository _repository = Substitute.For<IPositionRepository>();
    private readonly ListPositionsQueryHandler _handler;

    public ListPositionsQueryHandlerTests()
    {
        _handler = new ListPositionsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsAllPositions_WhenNoFilterApplied()
    {
        var tenantId = Guid.NewGuid();
        var active = TestPosition.Create(tenantId);
        active.Activate(TestPosition.NowUtc);
        var draft = TestPosition.Create(tenantId);
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([active, draft]);

        var result = await _handler.Handle(new ListPositionsQuery(tenantId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_FiltersByStatus_WhenStatusFilterApplied()
    {
        var tenantId = Guid.NewGuid();
        var active = TestPosition.Create(tenantId);
        active.Activate(TestPosition.NowUtc);
        var draft = TestPosition.Create(tenantId);
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([active, draft]);

        var result = await _handler.Handle(new ListPositionsQuery(tenantId, PositionStatus.Active, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(p => p.PositionId == active.Id.Value);
    }

    [Fact]
    public async Task Handle_FiltersByVacancy_WhenVacancyFilterApplied()
    {
        var tenantId = Guid.NewGuid();
        var filled = TestPosition.Create(tenantId);
        filled.MarkFilled(TestPosition.NowUtc);
        var vacant = TestPosition.Create(tenantId);
        _repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([filled, vacant]);

        var result = await _handler.Handle(new ListPositionsQuery(tenantId, null, VacancyStatus.Filled), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(p => p.PositionId == filled.Id.Value);
    }
}

public sealed class GetJobFamilyQueryHandlerTests
{
    private readonly IJobFamilyRepository _repository = Substitute.For<IJobFamilyRepository>();
    private readonly GetJobFamilyQueryHandler _handler;

    public GetJobFamilyQueryHandlerTests()
    {
        _handler = new GetJobFamilyQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenFound()
    {
        var tenantId = Guid.NewGuid();
        var jobFamily = TestPosition.CreateJobFamily(tenantId);
        _repository.GetByIdAsync(jobFamily.Id, Arg.Any<CancellationToken>()).Returns(jobFamily);

        var result = await _handler.Handle(new GetJobFamilyQuery(jobFamily.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be("IT");
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<JobFamilyId>(), Arg.Any<CancellationToken>()).Returns((JobFamily?)null);

        var result = await _handler.Handle(new GetJobFamilyQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobFamilyNotFound);
    }
}

public sealed class ListJobFamiliesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSummaries()
    {
        var repository = Substitute.For<IJobFamilyRepository>();
        var tenantId = Guid.NewGuid();
        repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([TestPosition.CreateJobFamily(tenantId)]);
        var handler = new ListJobFamiliesQueryHandler(repository);

        var result = await handler.Handle(new ListJobFamiliesQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}

public sealed class GetJobClassificationQueryHandlerTests
{
    private readonly IJobClassificationRepository _repository = Substitute.For<IJobClassificationRepository>();
    private readonly GetJobClassificationQueryHandler _handler;

    public GetJobClassificationQueryHandlerTests()
    {
        _handler = new GetJobClassificationQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenFound()
    {
        var tenantId = Guid.NewGuid();
        var jobClassification = TestPosition.CreateJobClassification(tenantId);
        _repository.GetByIdAsync(jobClassification.Id, Arg.Any<CancellationToken>()).Returns(jobClassification);

        var result = await _handler.Handle(new GetJobClassificationQuery(jobClassification.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<JobClassificationId>(), Arg.Any<CancellationToken>()).Returns((JobClassification?)null);

        var result = await _handler.Handle(new GetJobClassificationQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobClassificationNotFound);
    }
}

public sealed class ListJobClassificationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSummaries()
    {
        var repository = Substitute.For<IJobClassificationRepository>();
        var tenantId = Guid.NewGuid();
        repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([TestPosition.CreateJobClassification(tenantId)]);
        var handler = new ListJobClassificationsQueryHandler(repository);

        var result = await handler.Handle(new ListJobClassificationsQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}

public sealed class GetJobGradeQueryHandlerTests
{
    private readonly IJobGradeRepository _repository = Substitute.For<IJobGradeRepository>();
    private readonly GetJobGradeQueryHandler _handler;

    public GetJobGradeQueryHandlerTests()
    {
        _handler = new GetJobGradeQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_Succeeds_WhenFound()
    {
        var tenantId = Guid.NewGuid();
        var jobGrade = TestPosition.CreateJobGrade(tenantId);
        _repository.GetByIdAsync(jobGrade.Id, Arg.Any<CancellationToken>()).Returns(jobGrade);

        var result = await _handler.Handle(new GetJobGradeQuery(jobGrade.Id.Value, tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrganizationalLevel.Should().Be(8);
    }

    [Fact]
    public async Task Handle_Fails_WhenNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<JobGradeId>(), Arg.Any<CancellationToken>()).Returns((JobGrade?)null);

        var result = await _handler.Handle(new GetJobGradeQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PositionErrors.JobGradeNotFound);
    }
}

public sealed class ListJobGradesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSummaries()
    {
        var repository = Substitute.For<IJobGradeRepository>();
        var tenantId = Guid.NewGuid();
        repository.ListByTenantAsync(tenantId, Arg.Any<CancellationToken>()).Returns([TestPosition.CreateJobGrade(tenantId)]);
        var handler = new ListJobGradesQueryHandler(repository);

        var result = await handler.Handle(new ListJobGradesQuery(tenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
    }
}
