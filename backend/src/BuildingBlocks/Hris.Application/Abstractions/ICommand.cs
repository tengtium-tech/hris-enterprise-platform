using MediatR;

namespace Hris.Application.Abstractions;

/// <summary>
/// Marker distinguishing a state-changing request from an <see cref="IQuery{TResponse}"/>,
/// per application-pipeline.md's Transaction Behavior ("Commands execute within a
/// transaction... Queries generally execute without transactions").
///
/// <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/> is registered as an
/// open generic constrained to this interface, so Microsoft.Extensions.DependencyInjection
/// skips it entirely for any request that is not an <see cref="ICommand{TResponse}"/> --
/// a Query is never wrapped in a transaction by construction, not by a convention a
/// future handler could forget to follow.
/// </summary>
public interface ICommand<TResponse> : IRequest<TResponse>;
