using TuringSimulator.GameFlow.Events;
using UnityEngine;

/// <summary>
/// Executes agent actions by driving dialogue/TTS endpoints.
/// </summary>
public sealed class AgentActionExecutor : MonoBehaviour
{
    [SerializeField] private AgentActionRequestedEventChannel _agentActionChannel;
    [SerializeField] private AgentDialogue _agentDialogue;
    [SerializeField] private AgentTTS _agentTts;

    void OnEnable()
    {
        if (_agentActionChannel != null)
            _agentActionChannel.OnRaised += HandleActionRequested;
    }

    void OnDisable()
    {
        if (_agentActionChannel != null)
            _agentActionChannel.OnRaised -= HandleActionRequested;
    }

    void HandleActionRequested(AgentActionRequestedEventData eventData)
    {
        var dialogue = _agentDialogue != null ? _agentDialogue : AgentDialogue.Instance;
        var tts = _agentTts != null ? _agentTts : AgentTTS.Instance;

        if (dialogue != null)
            dialogue.SetThinkingState(eventData.Animation == AgentAnimationKind.Thinking);

        if (string.IsNullOrWhiteSpace(eventData.Text))
            return;

        if (dialogue != null)
            dialogue.ShowSubtitle(eventData.Text);
        tts?.Speak(eventData.Text);
    }
}
