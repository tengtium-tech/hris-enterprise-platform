using Hris.SharedKernel;

namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// Identity of the <see cref="ConfigurationSetting"/> Aggregate Root -- one logical,
/// scoped setting (e.g. "Payroll.GracePeriodMinutes" at Tenant scope), owning every
/// <see cref="ConfigurationVersion"/> ever published for it. Source:
/// docs/03-foundation/configuration-framework.md.
/// </summary>
public readonly record struct ConfigurationId(Guid Value) : IStronglyTypedId;
