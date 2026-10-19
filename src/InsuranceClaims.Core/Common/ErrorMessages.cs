namespace InsuranceClaims.Core.Common;

public static class ErrorMessages
{
    /// <summary>Returned with 409 when another employee won the race for a claim.</summary>
    public const string ClaimTakenByAnother = "התביעה נלקחה לטיפול על ידי עובד אחר";
}
