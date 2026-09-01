using FluentValidation;
using Hris.Foundation.Audit.Application.Queries;

namespace Hris.Foundation.Audit.Application.Validators;

/// <summary>
/// application-pipeline.md's Validation Behavior scope: "Required fields... Business-
/// independent validation." <c>IAuditRecorder</c> has no validator here since it is
/// not a MediatR request -- FluentValidation's pipeline behavior only ever runs
/// against something dispatched through the mediator, per that interface's own
/// remarks on why it is called directly instead.
/// </summary>
public sealed class SearchAuditRecordsQueryValidator : AbstractValidator<SearchAuditRecordsQuery>
{
    public SearchAuditRecordsQueryValidator()
    {
        RuleFor(q => q.RequestingPrincipalId).NotEmpty();
        RuleFor(q => q.ScopeLevel).IsInEnum();
        RuleFor(q => q.ScopeId).NotEmpty();
        RuleFor(q => q.PageNumber).GreaterThan(0);
        RuleFor(q => q.PageSize).GreaterThan(0);
    }
}

public sealed class GetAuditRecordByIdQueryValidator : AbstractValidator<GetAuditRecordByIdQuery>
{
    public GetAuditRecordByIdQueryValidator()
    {
        RuleFor(q => q.RequestingPrincipalId).NotEmpty();
        RuleFor(q => q.ScopeLevel).IsInEnum();
        RuleFor(q => q.ScopeId).NotEmpty();
        RuleFor(q => q.AuditRecordId).NotEmpty();
    }
}
