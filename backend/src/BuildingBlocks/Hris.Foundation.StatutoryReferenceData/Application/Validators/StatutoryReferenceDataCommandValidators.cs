using FluentValidation;
using Hris.Foundation.StatutoryReferenceData.Application.Commands;
using Hris.Foundation.StatutoryReferenceData.Application.Queries;

namespace Hris.Foundation.StatutoryReferenceData.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the Domain
/// layer's own factory/transition methods already enforce (code/country/version-label
/// shape, JSON well-formedness, signoff-already-recorded gating) -- the identical
/// separation every other framework's own validators file states for its own set.
/// </summary>
public sealed class RegisterStatutoryProgramCommandValidator : AbstractValidator<RegisterStatutoryProgramCommand>
{
    public RegisterStatutoryProgramCommandValidator()
    {
        RuleFor(c => c.Code).NotEmpty();
        RuleFor(c => c.Country).NotEmpty();
        RuleFor(c => c.DisplayName).NotEmpty();
    }
}

public sealed class PublishStatutoryTableVersionCommandValidator : AbstractValidator<PublishStatutoryTableVersionCommand>
{
    public PublishStatutoryTableVersionCommandValidator()
    {
        RuleFor(c => c.StatutoryProgramId).NotEmpty();
        RuleFor(c => c.VersionLabel).NotEmpty();
        RuleFor(c => c.IssuingAuthority).NotEmpty();
        RuleFor(c => c.IssuanceReference).NotEmpty();
        RuleFor(c => c.ScheduleData).NotEmpty();
    }
}

public sealed class RecordStatutoryTableVersionSignoffCommandValidator
    : AbstractValidator<RecordStatutoryTableVersionSignoffCommand>
{
    public RecordStatutoryTableVersionSignoffCommandValidator()
    {
        RuleFor(c => c.StatutoryTableVersionId).NotEmpty();
        RuleFor(c => c.SignoffBy).NotEmpty();
    }
}

public sealed class GetStatutoryProgramQueryValidator : AbstractValidator<GetStatutoryProgramQuery>
{
    public GetStatutoryProgramQueryValidator()
    {
        RuleFor(q => q.StatutoryProgramId).NotEmpty();
    }
}

public sealed class ListStatutoryProgramsQueryValidator : AbstractValidator<ListStatutoryProgramsQuery>
{
    public ListStatutoryProgramsQueryValidator()
    {
        RuleFor(q => q.Country).NotEmpty();
    }
}

public sealed class GetEffectiveStatutoryTableVersionQueryValidator : AbstractValidator<GetEffectiveStatutoryTableVersionQuery>
{
    public GetEffectiveStatutoryTableVersionQueryValidator()
    {
        RuleFor(q => q.ProgramCode).NotEmpty();
        RuleFor(q => q.Country).NotEmpty();
    }
}

public sealed class ListStatutoryTableVersionHistoryQueryValidator : AbstractValidator<ListStatutoryTableVersionHistoryQuery>
{
    public ListStatutoryTableVersionHistoryQueryValidator()
    {
        RuleFor(q => q.StatutoryProgramId).NotEmpty();
    }
}
