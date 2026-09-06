using FluentAssertions;
using Hris.Foundation.DocumentManagement.Application.Queries;
using Hris.Foundation.DocumentManagement.Domain;
using NSubstitute;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Application;

public sealed class ListDocumentAttachmentsQueryHandlerTests
{
    private readonly IDocumentAttachmentRepository _repository = Substitute.For<IDocumentAttachmentRepository>();
    private readonly ListDocumentAttachmentsQueryHandler _handler;

    public ListDocumentAttachmentsQueryHandlerTests()
    {
        _handler = new ListDocumentAttachmentsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_ReturnsEveryAttachment_ForTheGivenBusinessEntity()
    {
        var entityId = Guid.NewGuid();
        var attachment = DocumentAttachment.Attach(
            new DocumentId(Guid.NewGuid()), TestData.TenantId, "EmployeeProfile", entityId, Guid.NewGuid(), TestData.NowUtc).Value;
        _repository.ListByEntityAsync(TestData.TenantId, "EmployeeProfile", entityId, Arg.Any<CancellationToken>())
            .Returns([attachment]);

        var result = await _handler.Handle(
            new ListDocumentAttachmentsQuery(TestData.TenantId, "EmployeeProfile", entityId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Should().ContainSingle(dto => dto.DocumentAttachmentId == attachment.Id.Value).Subject;
        dto.DocumentId.Should().Be(attachment.DocumentId.Value);
        dto.EntityType.Should().Be("EmployeeProfile");
        dto.EntityId.Should().Be(entityId);
        dto.AttachedByUserId.Should().Be(attachment.AttachedByUserId);
        dto.AttachedAtUtc.Should().Be(attachment.AttachedAtUtc);
        dto.Status.Should().Be(attachment.Status.ToString());
        dto.DetachedAtUtc.Should().BeNull();
    }
}
