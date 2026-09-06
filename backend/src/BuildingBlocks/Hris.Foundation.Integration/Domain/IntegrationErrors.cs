using Hris.SharedKernel;

namespace Hris.Foundation.Integration.Domain;

/// <summary>
/// This bounded context's own reusable error catalog, per error-pattern.md's "Error
/// Catalog" section. <see cref="ConnectorNotFound"/>/<see cref="IntegrationRunNotFound"/>
/// are each the one error returned both for a genuinely missing record and for one
/// that exists but belongs to another tenant (CTR-ISO-004: background work "must be
/// subject to the same isolation as interactive requests") -- there is no separate
/// "wrong tenant" error for exactly this reason.
/// </summary>
public static class IntegrationErrors
{
    public static readonly Error NameRequired = new(
        "Integration.NameRequired",
        "A connector name is required.",
        ErrorCategory.Validation);

    public static readonly Error IntegrationCategoryRequired = new(
        "Integration.IntegrationCategoryRequired",
        "An integration category is required.",
        ErrorCategory.Validation);

    public static readonly Error EndpointAddressRequired = new(
        "Integration.EndpointAddressRequired",
        "An endpoint address is required.",
        ErrorCategory.Validation);

    public static readonly Error InvalidEndpointType = new(
        "Integration.InvalidEndpointType",
        "The given endpoint type is not a recognized value.",
        ErrorCategory.Validation);

    public static readonly Error InvalidTransformationType = new(
        "Integration.InvalidTransformationType",
        "The given transformation type is not a recognized value.",
        ErrorCategory.Validation);

    public static readonly Error InvalidRunKind = new(
        "Integration.InvalidRunKind",
        "The given integration run kind is not a recognized value.",
        ErrorCategory.Validation);

    public static readonly Error ConnectorNotFound = new(
        "Integration.ConnectorNotFound",
        "No connector exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidConnectorLifecycleTransition = new(
        "Integration.InvalidConnectorLifecycleTransition",
        "This transition is not valid from the connector's current status.",
        ErrorCategory.Domain);

    public static readonly Error SourceFieldRequired = new(
        "Integration.SourceFieldRequired",
        "A source field is required for a data mapping.",
        ErrorCategory.Validation);

    public static readonly Error TargetFieldRequired = new(
        "Integration.TargetFieldRequired",
        "A target field is required for a data mapping.",
        ErrorCategory.Validation);

    public static readonly Error DataMappingNotFound = new(
        "Integration.DataMappingNotFound",
        "No data mapping exists for the given identifier on this connector.",
        ErrorCategory.NotFound);

    public static readonly Error IntegrationRunNotFound = new(
        "Integration.IntegrationRunNotFound",
        "No integration run exists for the given identifier.",
        ErrorCategory.NotFound);

    public static readonly Error InvalidIntegrationRunLifecycleTransition = new(
        "Integration.InvalidIntegrationRunLifecycleTransition",
        "This transition is not valid from the integration run's current status.",
        ErrorCategory.Domain);

    public static readonly Error FailureReasonRequired = new(
        "Integration.FailureReasonRequired",
        "A failure reason is required to fail an integration run.",
        ErrorCategory.Validation);

    public static readonly Error RetryLimitExceeded = new(
        "Integration.RetryLimitExceeded",
        "This integration run has already reached its maximum retry count.",
        ErrorCategory.Domain);
}
