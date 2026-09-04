using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// Identity of the <see cref="StatutoryProgram"/> Aggregate Root -- one government
/// program (SSS, PhilHealth, Pag-IBIG, BIR withholding, GSIS, and so on) scoped to one
/// country, per statutory-reference-data.md's own Country Scoping tree ("Philippines
/// -&gt; SSS, PhilHealth, Pag-IBIG, BIR withholding, Regional minimum wage").
/// </summary>
public readonly record struct StatutoryProgramId(Guid Value) : IStronglyTypedId;
