using Hris.Application.Abstractions;
using Hris.Foundation.Extension.Application.Dtos;
using Hris.Foundation.Extension.Application.Mapping;
using Hris.Foundation.Extension.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Extension.Application.Queries;

/// <summary>
/// Every Hook registered against one Extension Point -- what a future execution
/// engine would need to invoke when that point fires, and what a platform operator
/// reviewing an extension point's own current subscribers would read.
/// </summary>
public sealed record ListHooksForExtensionPointQuery(Guid ExtensionPointId) : IQuery<Result<IReadOnlyCollection<HookDto>>>;

internal sealed class ListHooksForExtensionPointQueryHandler
    : IRequestHandler<ListHooksForExtensionPointQuery, Result<IReadOnlyCollection<HookDto>>>
{
    private readonly IHookRepository _repository;

    public ListHooksForExtensionPointQueryHandler(IHookRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<IReadOnlyCollection<HookDto>>> Handle(
        ListHooksForExtensionPointQuery request,
        CancellationToken cancellationToken)
    {
        var hooks = await _repository.GetByExtensionPointIdAsync(new ExtensionPointId(request.ExtensionPointId), cancellationToken).ConfigureAwait(false);
        IReadOnlyCollection<HookDto> dtos = hooks.Select(ExtensionMapper.ToDto).ToList();
        return Result.Success(dtos);
    }
}
