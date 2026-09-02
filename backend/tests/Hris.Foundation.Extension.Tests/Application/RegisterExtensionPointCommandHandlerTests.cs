using FluentAssertions;
using Hris.Foundation.Extension.Application.Commands;
using Hris.Foundation.Extension.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

public sealed class RegisterExtensionPointCommandHandlerTests
{
    private readonly IExtensionPointRepository _repository = Substitute.For<IExtensionPointRepository>();
    private readonly RegisterExtensionPointCommandHandler _handler;

    public RegisterExtensionPointCommandHandlerTests()
    {
        _handler = new RegisterExtensionPointCommandHandler(_repository, new FakeTimeProvider(TestData.NowUtc));
    }

    private static RegisterExtensionPointCommand ValidCommand(string key = "employee.before-save") => new(
        key, "Before Employee Save", "desc", ExtensionPointType.BusinessLogic, "Employee", [HookType.Before]);

    [Fact]
    public async Task Handle_Succeeds_AndPersistsTheNewExtensionPoint_WhenKeyIsAvailable()
    {
        _repository.ExistsByKeyAsync(Arg.Any<ExtensionPointKey>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).AddAsync(Arg.Any<ExtensionPoint>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenKeyIsAlreadyRegistered()
    {
        _repository.ExistsByKeyAsync(Arg.Any<ExtensionPointKey>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointKeyAlreadyRegistered);
        await _repository.DidNotReceive().AddAsync(Arg.Any<ExtensionPoint>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenKeyIsInvalid_WithoutCallingTheRepository()
    {
        var result = await _handler.Handle(ValidCommand(key: string.Empty), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.ExtensionPointKeyRequired);
        await _repository.DidNotReceive().ExistsByKeyAsync(Arg.Any<ExtensionPointKey>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Fails_WhenNameIsEmpty()
    {
        _repository.ExistsByKeyAsync(Arg.Any<ExtensionPointKey>(), Arg.Any<CancellationToken>()).Returns(false);

        var command = ValidCommand() with { Name = string.Empty };
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ExtensionErrors.NameRequired);
    }
}
