using FluentAssertions;
using Hris.Modules.Organization.Application.Commands;
using Hris.Modules.Organization.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Modules.Organization.Tests.Application;

public sealed class CreateLegalEntityCommandHandlerTests
{
    private readonly ILegalEntityRepository _repository = Substitute.For<ILegalEntityRepository>();
    private readonly CreateLegalEntityCommandHandler _handler;

    public CreateLegalEntityCommandHandlerTests()
    {
        _handler = new CreateLegalEntityCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenBusinessRegistrationNumberIsUnique()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithBusinessRegistrationNumberAsync("REG-1", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(
            new CreateLegalEntityCommand(
                tenantId, "TTS", "TengTium Software Inc.", null, "REG-1", null, "Philippines", null, null, null, null, null,
                null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<LegalEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenBusinessRegistrationNumberAlreadyExists()
    {
        var tenantId = Guid.NewGuid();
        _repository.ExistsWithBusinessRegistrationNumberAsync("REG-1", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(
            new CreateLegalEntityCommand(
                tenantId, "TTS", "TengTium Software Inc.", null, "REG-1", null, "Philippines", null, null, null, null, null,
                null, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(OrganizationErrors.DuplicateBusinessRegistrationNumber);
    }
}

public sealed class UpdateLegalEntityCommandHandlerTests
{
    private readonly ILegalEntityRepository _repository = Substitute.For<ILegalEntityRepository>();
    private readonly UpdateLegalEntityCommandHandler _handler;

    public UpdateLegalEntityCommandHandlerTests()
    {
        _handler = new UpdateLegalEntityCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenLegalEntityExists()
    {
        var legalEntity = TestLegalEntity.Create();
        _repository.GetByIdAsync(legalEntity.Id, Arg.Any<CancellationToken>()).Returns(legalEntity);

        var result = await _handler.Handle(
            new UpdateLegalEntityCommand(
                legalEntity.Id.Value, legalEntity.TenantId, "Renamed", null, null, "Philippines", null, null, null, null,
                null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        legalEntity.Name.Should().Be("Renamed");
    }
}

public sealed class ArchiveLegalEntityCommandHandlerTests
{
    private readonly ILegalEntityRepository _repository = Substitute.For<ILegalEntityRepository>();
    private readonly ArchiveLegalEntityCommandHandler _handler;

    public ArchiveLegalEntityCommandHandlerTests()
    {
        _handler = new ArchiveLegalEntityCommandHandler(_repository, new FakeTimeProvider(TestOrganization.NowUtc));
    }

    [Fact]
    public async Task Handle_Succeeds_WhenLegalEntityExists()
    {
        var legalEntity = TestLegalEntity.Create();
        _repository.GetByIdAsync(legalEntity.Id, Arg.Any<CancellationToken>()).Returns(legalEntity);

        var result = await _handler.Handle(
            new ArchiveLegalEntityCommand(legalEntity.Id.Value, legalEntity.TenantId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

internal static class TestLegalEntity
{
    public static LegalEntity Create() =>
        LegalEntity.Create(
            new LegalEntityId(Guid.NewGuid()), Guid.NewGuid(), "TTS", "TengTium Software Inc.", null, "REG-1", null,
            "Philippines", null, null, TestOrganization.NowUtc).Value;
}
