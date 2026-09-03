using FluentValidation;
using Hris.Foundation.Numbering.Application.Commands;
using Hris.Foundation.Numbering.Application.Queries;

namespace Hris.Foundation.Numbering.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields...
/// Business-independent validation." Deliberately does not re-check anything the
/// Domain layer's own factory/transition methods already enforce (key/prefix shape,
/// lifecycle-state gating, format-vs-mismatch comparison) -- the identical separation
/// every other framework's own validators file states for its own set.
/// </summary>
public sealed class RegisterNumberSeriesCommandValidator : AbstractValidator<RegisterNumberSeriesCommand>
{
    public RegisterNumberSeriesCommandValidator()
    {
        RuleFor(c => c.Key).NotEmpty();
        RuleFor(c => c.Prefix).NotEmpty();
        RuleFor(c => c.Separator).NotEmpty();
    }
}

public sealed class UpdateNumberSeriesFormatCommandValidator : AbstractValidator<UpdateNumberSeriesFormatCommand>
{
    public UpdateNumberSeriesFormatCommandValidator()
    {
        RuleFor(c => c.NumberSeriesId).NotEmpty();
        RuleFor(c => c.Prefix).NotEmpty();
        RuleFor(c => c.Separator).NotEmpty();
    }
}

public sealed class ResetSequenceCommandValidator : AbstractValidator<ResetSequenceCommand>
{
    public ResetSequenceCommandValidator()
    {
        RuleFor(c => c.NumberSeriesId).NotEmpty();
    }
}

public sealed class RequestAndReserveNumberCommandValidator : AbstractValidator<RequestAndReserveNumberCommand>
{
    public RequestAndReserveNumberCommandValidator()
    {
        RuleFor(c => c.NumberSeriesId).NotEmpty();
    }
}

public sealed class ConfirmNumberGeneratedCommandValidator : AbstractValidator<ConfirmNumberGeneratedCommand>
{
    public ConfirmNumberGeneratedCommandValidator()
    {
        RuleFor(c => c.IssuedNumberId).NotEmpty();
    }
}

public sealed class AssignNumberCommandValidator : AbstractValidator<AssignNumberCommand>
{
    public AssignNumberCommandValidator()
    {
        RuleFor(c => c.IssuedNumberId).NotEmpty();
        RuleFor(c => c.AssignedToType).NotEmpty();
        RuleFor(c => c.AssignedToReferenceId).NotEmpty();
    }
}

public sealed class ValidateNumberCommandValidator : AbstractValidator<ValidateNumberCommand>
{
    public ValidateNumberCommandValidator()
    {
        RuleFor(c => c.IssuedNumberId).NotEmpty();
    }
}

public sealed class ReleaseNumberCommandValidator : AbstractValidator<ReleaseNumberCommand>
{
    public ReleaseNumberCommandValidator()
    {
        RuleFor(c => c.IssuedNumberId).NotEmpty();
        RuleFor(c => c.Reason).NotEmpty();
    }
}

public sealed class ArchiveNumberCommandValidator : AbstractValidator<ArchiveNumberCommand>
{
    public ArchiveNumberCommandValidator()
    {
        RuleFor(c => c.IssuedNumberId).NotEmpty();
    }
}

public sealed class GetNumberSeriesQueryValidator : AbstractValidator<GetNumberSeriesQuery>
{
    public GetNumberSeriesQueryValidator()
    {
        RuleFor(q => q.Key).NotEmpty();
    }
}

public sealed class GetIssuedNumberQueryValidator : AbstractValidator<GetIssuedNumberQuery>
{
    public GetIssuedNumberQueryValidator()
    {
        RuleFor(q => q.IssuedNumberId).NotEmpty();
    }
}

public sealed class ListIssuedNumbersForSeriesQueryValidator : AbstractValidator<ListIssuedNumbersForSeriesQuery>
{
    public ListIssuedNumbersForSeriesQueryValidator()
    {
        RuleFor(q => q.NumberSeriesId).NotEmpty();
    }
}
