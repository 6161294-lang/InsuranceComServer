namespace InsuranceClaims.Core.Enums;

/// <summary>Lifecycle of an insurance claim.</summary>
public enum ClaimStatus
{
    Draft = 0,
    Opened = 1,
    InTreatment = 2,
    Approved = 3,
    Rejected = 4,
    Cancelled = 5
}
