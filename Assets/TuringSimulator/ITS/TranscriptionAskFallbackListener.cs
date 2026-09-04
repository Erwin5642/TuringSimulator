using TuringSimulator.GameFlow.Events;
using UnityEngine;

/// <summary>
/// When no ITS client is present to post <c>/ask</c>, raises a successful AskResult
/// whose reply is the radio fallback so the mapper/TTS still speak.
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
        var itsClientPresent = ResolveAskClient() != null;
        if (!TranscriptionAskFallback.ShouldPublishLocalFallback(eventData.Text, itsClientPresent))
            return;

        if (_askResultChannel == null)
        {
            Debug.LogWarning("[TranscriptionAskFallbackListener] Missing AskResult channel.", this);
            return;
        }

        var reply = TranscriptionAskFallback.UnreachableReply;
        var payload = new AskResultEventData(
            eventData.Context,
            success: true,
            reply: reply,
            error: string.Empty);
        _askResultChannel.Raise(payload, this);
        Debug.Log("[TranscriptionAskFallbackListener] No ITS client; using radio fallback reply.");
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
