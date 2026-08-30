using MediatR;

namespace Hris.Application.Abstractions;

/// <summary>
/// Marker for a read-only request, per application-pipeline.md's Query Handlers
/// section: "Execute read operations... Avoid loading unnecessary aggregates...
/// Queries must not modify system state." See <see cref="ICommand{TResponse}"/> for why
/// this distinction exists structurally rather than by naming convention alone.
/// </summary>
public interface IQuery<TResponse> : IRequest<TResponse>;
