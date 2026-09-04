/// <summary>
/// Radio-style tutor reply used when voice cannot reach the ITS API.
/// Raised as a successful <c>AskResult</c> so the agent treats it like a server reply.
/// </summary>
public static class TranscriptionAskFallback
{
    public const string UnreachableReply = "O rádio não ta muito bom.";

    /// <summary>
    /// True when there is no ITS client to post <c>/ask</c>, so a local AskResult
    /// must stand in for the server.
    /// </summary>
    public static bool ShouldPublishLocalFallback(string transcription, bool itsClientPresent)
    {
        return !itsClientPresent && !string.IsNullOrWhiteSpace(transcription);
    }
}
