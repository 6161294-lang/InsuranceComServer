namespace InsuranceClaims.Core.Enums;

/// <summary>Lifecycle of an insurance claim.</summary>
public enum ClaimStatus
{
    Open = 0,
    InProgress = 1,
    Approved = 2,
    Rejected = 3
}
