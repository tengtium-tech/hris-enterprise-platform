using Hris.SharedKernel;

namespace Hris.Foundation.Scheduling.Domain;

/// <summary>
/// Identity of the <see cref="Schedule"/> Aggregate Root, per scheduling-framework.md's
/// own Core Concepts ("A Schedule defines when a task should execute").
/// </summary>
public readonly record struct ScheduleId(Guid Value) : IStronglyTypedId;
