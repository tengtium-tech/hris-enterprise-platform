using Hris.Application.Abstractions;
using MediatR;

namespace Hris.Application.Behaviors;

/// <summary>
/// application-pipeline.md's Transaction Behavior: "Begin transaction, Execute
/// handler, Save changes, Commit transaction, Roll back on failure... Queries
/// generally execute without transactions."
///
/// Constrained to <typeparamref name="TRequest"/> : <see cref="ICommand{TResponse}"/>
/// so Microsoft.Extensions.DependencyInjection's open-generic resolution skips this
/// behavior entirely for a Query -- the "Queries generally execute without
/// transactions" half of the cited section is enforced by this constraint, not left to
/// a query handler's own discipline.
///
/// Does not call <c>Database.BeginTransaction()</c> explicitly: a single
/// <see cref="IUnitOfWork.SaveChangesAsync"/> call is already atomic, and
/// dbcontext-design.md's own Common Anti-Patterns section prohibits the one situation
/// (multiple <c>SaveChanges()</c> calls in one business transaction) that would make an
/// explicit ambient transaction necessary. If a future command genuinely needs more
/// than one <see cref="IUnitOfWork.SaveChangesAsync"/> call, that command's own design
/// should be revisited first, per that same anti-pattern -- not worked around by
/// wrapping this behavior in an ambient transaction.
///
/// A command handler that fails returns a failed <see cref="Hris.SharedKernel.Result"/>
/// (an expected business outcome, per result-pattern.md) rather than throwing, so
/// "roll back on failure" for that path means simply never calling
/// <see cref="IUnitOfWork.SaveChangesAsync"/> -- this behavior checks
/// <see cref="Hris.SharedKernel.Result.IsFailure"/> via reflection-free pattern matching
/// where the response is a <see cref="Hris.SharedKernel.Result"/>, and only persists on
/// success. An actual thrown exception propagates past this behavior uncommitted, which
/// is EF Core's own default (nothing is persisted unless SaveChanges is reached).
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next().ConfigureAwait(false);

        if (response is Hris.SharedKernel.Result { IsFailure: true })
        {
            return response;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return response;
    }
}
