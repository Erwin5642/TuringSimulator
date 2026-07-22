using TuringSimulator.GameFlow.Events;
using UnityEngine;

/// <summary>
/// Routes voice/thinking channel updates into AgentDialogue UI widgets.
/// </summary>
public sealed class AgentVoiceFeedbackListener : MonoBehaviour
{
    [SerializeField] private ListeningStateChangedEventChannel _listeningStateChannel;
    [SerializeField] private PartialTranscriptionEventChannel _partialTranscriptionChannel;
    [SerializeField] private ThinkingStateChangedEventChannel _thinkingStateChannel;
    [SerializeField] private AgentDialogue _agentDialogue;

    void OnEnable()
    {
        if (_listeningStateChannel != null)
            _listeningStateChannel.OnRaised += HandleListeningStateChanged;
        if (_partialTranscriptionChannel != null)
            _partialTranscriptionChannel.OnRaised += HandlePartialTranscription;
        if (_thinkingStateChannel != null)
            _thinkingStateChannel.OnRaised += HandleThinkingStateChanged;
    }

    void OnDisable()
    {
        if (_listeningStateChannel != null)
            _listeningStateChannel.OnRaised -= HandleListeningStateChanged;
        if (_partialTranscriptionChannel != null)
            _partialTranscriptionChannel.OnRaised -= HandlePartialTranscription;
        if (_thinkingStateChannel != null)
            _thinkingStateChannel.OnRaised -= HandleThinkingStateChanged;
    }

    void HandleListeningStateChanged(ListeningStateChangedEventData eventData)
    {
        var dialogue = ResolveDialogue();
        dialogue?.SetListeningState(eventData.IsListening);
        if (!eventData.IsListening)
            dialogue?.SetPartialTranscription(string.Empty);
    }

    void HandlePartialTranscription(PartialTranscriptionEventData eventData)
    {
        ResolveDialogue()?.SetPartialTranscription(eventData.PartialText);
    }

    void HandleThinkingStateChanged(ThinkingStateChangedEventData eventData)
    {
        ResolveDialogue()?.SetThinkingState(eventData.IsThinking);
    }

    AgentDialogue ResolveDialogue() =>
        _agentDialogue != null ? _agentDialogue : AgentDialogue.Instance;
}
