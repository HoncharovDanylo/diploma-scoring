namespace Origination.Domain;

public static class ApplicationStatuses
{
    public const string Submitted = "Submitted";
    public const string ScoringPending = "ScoringPending";
    public const string ScoringRejectedFinal = "ScoringRejectedFinal";
    public const string ManualVerificationPending = "ManualVerificationPending";
    public const string ManualApproved = "ManualApproved";
    public const string ManualRejected = "ManualRejected";
    public const string ScoringFailed = "ScoringFailed";
}
