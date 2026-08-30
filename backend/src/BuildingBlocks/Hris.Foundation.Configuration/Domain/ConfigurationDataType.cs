namespace Hris.Foundation.Configuration.Domain;

/// <summary>
/// The declared shape a <see cref="ConfigurationSetting"/>'s value must conform to.
/// configuration-framework.md's own Configuration Validation section requires "Data
/// Types" be validated but does not enumerate a concrete set; this is the minimal set
/// that covers every example the document itself gives (Session Timeout = Number,
/// MFA Policy enablement = Boolean, SMTP Server = Text, Password Policy = Json).
///
/// Named <see cref="Text"/> rather than <c>String</c> per CA1720 (an identifier
/// should not itself repeat a type name a consumer might import).
/// </summary>
public enum ConfigurationDataType
{
    Text = 0,
    Number,
    Boolean,
    Json,
}
