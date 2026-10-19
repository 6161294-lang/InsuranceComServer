namespace InsuranceClaims.Core.Common;

/// <summary>Outcome of a business operation. The API layer maps each value to an HTTP status code.</summary>
public enum ResultStatus
{
    Success,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    ValidationFailed
}
