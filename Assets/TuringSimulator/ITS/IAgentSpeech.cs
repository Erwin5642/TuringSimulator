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
    void Speak(string text);

    void Stop();
}
