using FluentAssertions;
using Hris.Foundation.Extension.Application.Queries;
using Hris.Foundation.Extension.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.Extension.Tests.Application;

public sealed class ListExtensionPointsQueryHandlerTests
{
    private readonly IExtensionPointRepository _repository = Substitute.For<IExtensionPointRepository>();
    private readonly ListExtensionPointsQueryHandler _handler;

    public ListExtensionPointsQueryHandlerTests()
    {
        _handler = new ListExtensionPointsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEveryRegisteredExtensionPoint()
    {
        IReadOnlyCollection<ExtensionPoint> extensionPoints = [TestData.RegisteredExtensionPoint(), TestData.PublishedExtensionPoint()];
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(extensionPoints);

        var result = await _handler.Handle(new ListExtensionPointsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ReturnsAnEmptyCollection_WhenNoExtensionPointsExist()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns((IReadOnlyCollection<ExtensionPoint>)[]);

        var result = await _handler.Handle(new ListExtensionPointsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
