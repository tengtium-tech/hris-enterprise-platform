using FluentAssertions;
using Hris.Foundation.Extension.Application.Queries;
using Hris.Foundation.Extension.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

public sealed class ListHooksForExtensionPointQueryHandlerTests
{
    private readonly IHookRepository _repository = Substitute.For<IHookRepository>();
    private readonly ListHooksForExtensionPointQueryHandler _handler;

    public ListHooksForExtensionPointQueryHandlerTests()
    {
        _handler = new ListHooksForExtensionPointQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEveryHookRegisteredAgainstTheExtensionPoint()
    {
        var extensionPointId = new ExtensionPointId(Guid.NewGuid());
        IReadOnlyCollection<Hook> hooks = [TestData.RegisteredHook(extensionPointId), TestData.RegisteredHook(extensionPointId)];
        _repository.GetByExtensionPointIdAsync(extensionPointId, Arg.Any<CancellationToken>()).Returns(hooks);

        var result = await _handler.Handle(new ListHooksForExtensionPointQuery(extensionPointId.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyCollection_WhenNoHooksAreRegistered()
    {
        _repository.GetByExtensionPointIdAsync(Arg.Any<ExtensionPointId>(), Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<Hook>)[]);

        var result = await _handler.Handle(new ListHooksForExtensionPointQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
