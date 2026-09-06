using Hris.SharedKernel;

namespace Hris.Foundation.DocumentManagement.Domain;

/// <summary>
/// Identity of one <see cref="DocumentVersion"/> child Entity within a
/// <see cref="Document"/>'s own <see cref="Document.Versions"/> history.
/// </summary>
public readonly record struct DocumentVersionId(Guid Value) : IStronglyTypedId;
