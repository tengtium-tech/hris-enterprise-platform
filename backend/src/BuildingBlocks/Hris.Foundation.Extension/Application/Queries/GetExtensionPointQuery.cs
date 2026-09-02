using Hris.Application.Abstractions;
using Hris.Foundation.Extension.Application.Dtos;
using Hris.Foundation.Extension.Application.Mapping;
using Hris.Foundation.Extension.Domain;
using Hris.SharedKernel;
using MediatR;

namespace Hris.Foundation.Extension.Application.Queries;

/// <summary>
/// Reads one extension point back by its own stable <see cref="ExtensionPointKey"/> --
/// the natural key a module registering a Hook actually has in hand, matching
/// <c>GetCountryConfigurationQuery</c>'s own identical by-natural-key shape.
/// </summary>
public sealed record GetExtensionPointQuery(string Key) : IQuery<Result<ExtensionPointDto>>;

internal sealed class GetExtensionPointQueryHandler : IRequestHandler<GetExtensionPointQuery, Result<ExtensionPointDto>>
{
    private readonly IExtensionPointRepository _repository;

    public GetExtensionPointQueryHandler(IExtensionPointRepository repository)
    {
        _repository = Guard.AgainstNull(repository, nameof(repository));
    }

    public async Task<Result<ExtensionPointDto>> Handle(GetExtensionPointQuery request, CancellationToken cancellationToken)
    {
        var keyResult = ExtensionPointKey.Create(request.Key);
        if (keyResult.IsFailure)
        {
            return Result.Failure<ExtensionPointDto>(keyResult.Error);
        }

        var extensionPoint = await _repository.GetByKeyAsync(keyResult.Value, cancellationToken).ConfigureAwait(false);

        return extensionPoint is null
            ? Result.Failure<ExtensionPointDto>(ExtensionErrors.ExtensionPointNotFound)
            : Result.Success(ExtensionMapper.ToDto(extensionPoint));
    }
}
