using System.Diagnostics.CodeAnalysis;
using MediatR;

namespace Hris.Application.Abstractions;

/// <summary>
/// Marker for a read-only request, per application-pipeline.md's Query Handlers
/// section: "Execute read operations... Avoid loading unnecessary aggregates...
/// Queries must not modify system state." See <see cref="ICommand{TResponse}"/> for why
/// this distinction exists structurally rather than by naming convention alone.
/// </summary>
[SuppressMessage(
    "Design",
    "CA1040:Avoid empty interfaces",
    Justification = "Deliberately empty, for the same generic-constraint reason " +
        "ICommand<TResponse> states in its own Justification -- TransactionBehavior's " +
        "constraint filters on ICommand, and this sibling interface exists so a Query can be " +
        "distinguished from \"anything that isn't specifically a Command\" rather than being " +
        "defined only by absence.")]
public interface IQuery<TResponse> : IRequest<TResponse>;
