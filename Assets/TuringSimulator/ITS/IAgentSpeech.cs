using System;

/// <summary>
/// Contract for agent speech playback. Implementation synthesizes via Wit TTS.
/// </summary>
public interface IAgentSpeech
{
    bool IsSpeaking { get; }

    event Action<string> OnSpeechStarted;
    event Action OnSpeechFinished;
    event Action<string> OnSpeechError;

    /// <param name="text">Spoken/subtitle text sent to Wit TTS.</param>
    /// <param name="audioUrl">Ignored. Kept so event payloads can still carry a URL.</param>
    void Speak(string text, string audioUrl = null);

    void Stop();
}
