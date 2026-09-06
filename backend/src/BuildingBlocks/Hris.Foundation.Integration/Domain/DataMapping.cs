using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// One configurable field mapping owned by a <see cref="Connector"/>. Source:
/// docs/03-foundation/integration-framework.md, Data Mapping ("The framework should
/// support configurable field mapping... Employee.EmployeeNumber -&gt;
/// ERP.PersonnelNumber... Mappings should avoid hard-coded transformations"). A child
/// Entity, never an Aggregate Root of its own (aggregate-design-rules.md Rule 7) --
/// its constructor is <c>internal</c>, reachable only through <see cref="Connector"/>,
/// the same shape <c>FileVersion</c>/<c>DocumentVersion</c> already establish for a
/// child collection owned by a single, non-population-scale parent.
///
/// <see cref="TransformationRule"/> is a plain, optional, opaque string -- a format
/// string, an expression, or a lookup-table reference -- rather than a structured rule
/// language, since evaluating one is Business Logic, and this document's own Scope
/// section excludes exactly that. This framework transforms and routes only, per its
/// own AI Implementation Guidance ("Never place business logic in an integration
/// adapter; transform and route only").
/// </summary>
public sealed class DataMapping : Entity<DataMappingId>
{
    public string SourceField { get; }

    public string TargetField { get; }

    public TransformationType TransformationType { get; }

    public string? TransformationRule { get; }

    internal DataMapping(
        DataMappingId id, string sourceField, string targetField, TransformationType transformationType, string? transformationRule)
        : base(id)
    {
        SourceField = sourceField;
        TargetField = targetField;
        TransformationType = transformationType;
        TransformationRule = transformationRule;
    }
}
