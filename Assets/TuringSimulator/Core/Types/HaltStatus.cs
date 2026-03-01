namespace TuringSimulator.Core.Types
{
    public enum HaltStatus
    {
        None,
        Accept,
        Reject,
        StepLimitExceeded,
        Aborted
    }
}