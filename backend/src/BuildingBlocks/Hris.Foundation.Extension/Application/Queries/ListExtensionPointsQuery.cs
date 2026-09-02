using Hris.Application.Abstractions;
using Hris.Foundation.Extension.Application.Dtos;
using Hris.Foundation.Extension.Application.Mapping;
using Hris.Foundation.Extension.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Extension.Application.Queries;

/// <summary>
/// Every registered extension point, per this framework's own registry role: "All
/// Business Modules" are this document's own stated Downstream Consumers, and a
/// future module deciding which points already exist before registering a new one
/// needs to list them all. Ungated, matching <c>RegisterExtensionPointCommand</c>'s
/// own remarks -- there is no tenant scope to check a platform-wide registry read
/// against.
/// </summary>
public sealed record ListExtensionPointsQuery : IQuery<Result<IReadOnlyCollection<ExtensionPointDto>>>;

internal sealed class ListExtensionPointsQueryHandler
    : IRequestHandler<ListExtensionPointsQuery, Result<IReadOnlyCollection<ExtensionPointDto>>>
{
    private readonly IExtensionPointRepository _repository;

    public ListExtensionPointsQueryHandler(IExtensionPointRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyCollection<ExtensionPointDto>>> Handle(
        ListExtensionPointsQuery request,
        CancellationToken cancellationToken)
    {
        var extensionPoints = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyCollection<ExtensionPointDto> dtos = extensionPoints.Select(ExtensionMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
