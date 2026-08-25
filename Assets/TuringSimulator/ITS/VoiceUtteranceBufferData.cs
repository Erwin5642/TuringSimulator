/// <summary>
/// Latest Meta Voice STT string for one listen session, plus when it arrived.
/// </summary>
public readonly struct VoiceUtteranceBufferData
{
    public static VoiceUtteranceBufferData Empty { get; } =
        new VoiceUtteranceBufferData(string.Empty, 0f);

    public readonly string AccumulatedText;
    public readonly float LastSpeechUnscaledTime;

    public VoiceUtteranceBufferData(string accumulatedText, float lastSpeechUnscaledTime)
    {
        AccumulatedText = accumulatedText ?? string.Empty;
        LastSpeechUnscaledTime = lastSpeechUnscaledTime;
    }

    public bool HasText => !string.IsNullOrWhiteSpace(AccumulatedText);
}
