using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Identity of the <see cref="IntegrationRun"/> Aggregate Root.
/// </summary>
public readonly record struct IntegrationRunId(Guid Value) : IStronglyTypedId;
