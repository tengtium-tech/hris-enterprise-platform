using Hris.SharedKernel;

namespace Hris.Foundation.Localization.Domain;

/// <summary>Identity of the <see cref="TranslationEntry"/> Aggregate Root.</summary>
public readonly record struct TranslationEntryId(Guid Value) : IStronglyTypedId;
