using Hris.SharedKernel;

namespace Hris.Foundation.StatutoryReferenceData.Domain;

/// <summary>
/// Identity of the <see cref="StatutoryTableVersion"/> Aggregate Root -- one
/// effective-dated, immutable-once-published version of a <see cref="StatutoryProgram"/>'s
/// own table, per statutory-reference-data.md's own Effective Dating section. Kept as
/// its own Aggregate Root rather than a child Entity of <see cref="StatutoryProgram"/>
/// for the identical reason <c>IssuedNumberId</c>'s own remarks give for
/// <c>IssuedNumber</c>: a program's own versions accumulate without bound across years
/// of statutory updates, and a version's own lifecycle (publish once, sign off once,
/// never edited again) does not share a consistency boundary with the program's own
/// registration.
/// </summary>
public readonly record struct StatutoryTableVersionId(Guid Value) : IStronglyTypedId;
