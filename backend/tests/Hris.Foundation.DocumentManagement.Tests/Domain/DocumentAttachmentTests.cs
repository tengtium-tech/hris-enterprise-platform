using FluentAssertions;
using Hris.Foundation.DocumentManagement.Domain;
using System.Linq;
using Xunit;

namespace Hris.Foundation.DocumentManagement.Tests.Domain;

public sealed class DocumentAttachmentTests
{
    [Fact]
    public void Attach_Succeeds_AndRaisesDocumentAttached()
    {
        var documentId = new DocumentId(Guid.NewGuid());
        var entityId = Guid.NewGuid();
        var attachedByUserId = Guid.NewGuid();

        var result = DocumentAttachment.Attach(
            documentId, TestData.TenantId, "EmployeeProfile", entityId, attachedByUserId, TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(DocumentAttachmentStatus.Attached);
        result.Value.AttachedByUserId.Should().Be(attachedByUserId);
        var raised = result.Value.DomainEvents.OfType<DocumentAttached>().Should().ContainSingle().Subject;
        raised.DocumentAttachmentId.Should().Be(result.Value.Id);
        raised.DocumentId.Should().Be(documentId);
        raised.TenantId.Should().Be(TestData.TenantId);
        raised.EntityType.Should().Be("EmployeeProfile");
        raised.EntityId.Should().Be(entityId);
    }

    [Fact]
    public void Attach_Fails_WhenEntityTypeIsEmpty()
    {
        var result = DocumentAttachment.Attach(
            new DocumentId(Guid.NewGuid()), TestData.TenantId, string.Empty, Guid.NewGuid(), Guid.NewGuid(), TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.EntityTypeRequired);
    }

    [Fact]
    public void Attach_Fails_WhenTenantIdIsEmpty()
    {
        var act = () => DocumentAttachment.Attach(
            new DocumentId(Guid.NewGuid()), Guid.Empty, "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid(), TestData.NowUtc);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Detach_Succeeds_FromAttached_AndRaisesNoEvent()
    {
        var attachment = DocumentAttachment.Attach(
            new DocumentId(Guid.NewGuid()), TestData.TenantId, "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid(), TestData.NowUtc).Value;
        attachment.ClearDomainEvents();

        var result = attachment.Detach(TestData.NowUtc);

        result.IsSuccess.Should().BeTrue();
        attachment.Status.Should().Be(DocumentAttachmentStatus.Detached);
        attachment.DetachedAtUtc.Should().Be(TestData.NowUtc);
        attachment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Detach_Fails_WhenAlreadyDetached()
    {
        var attachment = DocumentAttachment.Attach(
            new DocumentId(Guid.NewGuid()), TestData.TenantId, "EmployeeProfile", Guid.NewGuid(), Guid.NewGuid(), TestData.NowUtc).Value;
        attachment.Detach(TestData.NowUtc);

        var result = attachment.Detach(TestData.NowUtc);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(DocumentErrors.InvalidDocumentAttachmentTransition);
    }
}
