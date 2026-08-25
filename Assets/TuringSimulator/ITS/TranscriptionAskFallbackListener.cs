using TuringSimulator.GameFlow.Events;
using UnityEngine;

/// <summary>
/// When <c>/ask</c> cannot be posted, raises a successful AskResult whose reply
/// is the STT text so the mapper/TTS speak exactly what was heard.
/// </summary>
public sealed class TranscriptionAskFallbackListener : MonoBehaviour
{
    [SerializeField] private TranscriptionReadyEventChannel _transcriptionReadyChannel;
    [SerializeField] private AskResultEventChannel _askResultChannel;
    [SerializeField] private ITSClient _askClient;

    void OnEnable()
    {
        if (_transcriptionReadyChannel != null)
            _transcriptionReadyChannel.OnRaised += HandleTranscriptionReady;
    }

    void OnDisable()
    {
        if (_transcriptionReadyChannel != null)
            _transcriptionReadyChannel.OnRaised -= HandleTranscriptionReady;
    }

    void HandleTranscriptionReady(TranscriptionReadyEventData eventData)
    {
        var canPostAsk = ResolveAskClient()?.CanPostAsk ?? false;
        if (!TranscriptionAskFallback.ShouldEcho(eventData.Text, canPostAsk))
            return;

        if (_askResultChannel == null)
        {
            Debug.LogWarning("[TranscriptionAskFallbackListener] Missing AskResult channel.", this);
            return;
        }

        var text = TranscriptionAskFallback.ResolveEchoText(eventData.Text);
        var payload = new AskResultEventData(
            eventData.Context,
            success: true,
            reply: text,
            error: string.Empty);
        _askResultChannel.Raise(payload, this);
        Debug.Log($"[TranscriptionAskFallbackListener] Echoing STT via TTS chars={text.Length}.");
    }

    IAskClient ResolveAskClient()
    {
        if (_askClient != null)
            return _askClient;
        return ITSClient.Instance;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_transcriptionReadyChannel == null)
            Debug.LogWarning($"{name}: assign TranscriptionReadyEventChannel.", this);
        if (_askResultChannel == null)
            Debug.LogWarning($"{name}: assign AskResultEventChannel.", this);
    }
#endif
}
