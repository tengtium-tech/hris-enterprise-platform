using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Identity of the <see cref="DocumentAttachment"/> Aggregate Root.
/// </summary>
public readonly record struct DocumentAttachmentId(Guid Value) : IStronglyTypedId;
