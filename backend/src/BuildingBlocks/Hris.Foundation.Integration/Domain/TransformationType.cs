namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// Source: docs/03-foundation/integration-framework.md, Transformation ("Transformation
/// may include: Field Mapping, Type Conversion, Value Translation, Aggregation,
/// Splitting, Validation, Enrichment"). A closed enum -- this is a fixed technical
/// taxonomy of transformation kinds, not a business-invented category; the actual
/// per-field rule (a format string, an expression, a lookup-table reference) is kept
/// as an opaque <see cref="DataMapping.TransformationRule"/> string rather than a
/// structured rules language, since building a transformation-rule evaluator is out
/// of this Sprint's own scope (this document's own Scope section excludes "Business
/// Logic").
/// </summary>
public enum TransformationType
{
    FieldMapping = 0,
    TypeConversion,
    ValueTranslation,
    Aggregation,
    Splitting,
    Validation,
    Enrichment,
}
