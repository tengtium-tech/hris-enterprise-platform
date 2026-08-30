namespace Hris.Foundation.Identity.Domain;

/// <summary>
/// The six factor types identity-framework.md's Multi-Factor Authentication section
/// names: "Authenticator Applications, SMS OTP, Email OTP, Hardware Security Keys,
/// Biometrics, Push Notifications." Bounds the same document's own
/// <c>EnrollMfaFactorCommand</c> ("Factor type (bounded by the Multi-Factor
/// Authentication section's own supported-factor list)").
/// </summary>
public enum MfaFactorType
{
    AuthenticatorApp = 0,
    SmsOtp,
    EmailOtp,
    HardwareSecurityKey,
    Biometric,
    PushNotification,
}
