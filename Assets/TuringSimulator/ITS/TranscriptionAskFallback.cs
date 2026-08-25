/// <summary>
/// Decides when a finalized STT utterance should be echoed as the tutor reply
/// instead of posting <c>/ask</c>.
/// </summary>
public static class TranscriptionAskFallback
{
    public static bool ShouldEcho(string transcription, bool canPostAsk)
    {
        return !canPostAsk && !string.IsNullOrWhiteSpace(transcription);
    }

    public static string ResolveEchoText(string transcription) =>
        transcription?.Trim() ?? string.Empty;
}
