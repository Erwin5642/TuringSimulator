/// <summary>
/// Pure rules for when Wit STT may be sent to <c>/ask</c> / TTS.
/// The text is whatever Meta Voice last emitted; this only delays the commit.
/// </summary>
public static class VoiceTranscriptionCommit
{
    public static VoiceUtteranceBufferData Capture(string incoming, float nowUnscaled)
    {
        if (string.IsNullOrWhiteSpace(incoming))
            return VoiceUtteranceBufferData.Empty;

        return new VoiceUtteranceBufferData(incoming.Trim(), nowUnscaled);
    }

    public static bool ShouldCommitOnSilence(
        in VoiceUtteranceBufferData buffer,
        float nowUnscaled,
        float silenceSeconds)
    {
        if (silenceSeconds <= 0f || !buffer.HasText)
            return false;

        return nowUnscaled - buffer.LastSpeechUnscaledTime >= silenceSeconds;
    }

    public static string ResolveCommitText(in VoiceUtteranceBufferData buffer) =>
        buffer.AccumulatedText?.Trim() ?? string.Empty;
}
