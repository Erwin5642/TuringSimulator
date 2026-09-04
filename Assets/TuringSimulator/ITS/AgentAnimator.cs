using TuringSimulator.GameFlow.Events;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class AgentAnimator : MonoBehaviour
{
    [Header("Event Channel")]
    [SerializeField] private AgentActionRequestedEventChannel _agentActionChannel;

    [Header("Animator Params")]
    [SerializeField] private string _idleBool = "Idle";
    [SerializeField] private string _thinkingBool = "Thinking";
    [SerializeField] private string _talkingBool = "Talking";
    [SerializeField] private string _celebrateTrigger = "Celebrate";

    [Header("Subtitle")]
    [SerializeField] private AgentDialogue _agentDialogue;

    private Animator _animator;
    private int _idleHash;
    private int _thinkingHash;
    private int _talkingHash;
    private int _celebrateHash;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        RebuildHashes();
    }

    void OnEnable()
    {
        if (_agentActionChannel != null)
            _agentActionChannel.OnRaised += HandleActionRequested;

        if (AgentTTS.Instance != null)
            AgentTTS.Instance.OnSpeechFinished += HandleSpeechFinished;

        var dialogue = ResolveDialogue();
        if (dialogue != null)
            dialogue.OnSubtitleDismissed += HandleSubtitleDismissed;
    }

    void OnDisable()
    {
        if (_agentActionChannel != null)
            _agentActionChannel.OnRaised -= HandleActionRequested;

        if (AgentTTS.Instance != null)
            AgentTTS.Instance.OnSpeechFinished -= HandleSpeechFinished;

        var dialogue = ResolveDialogue();
        if (dialogue != null)
            dialogue.OnSubtitleDismissed -= HandleSubtitleDismissed;
    }

    void HandleActionRequested(AgentActionRequestedEventData eventData)
    {
        if (_animator == null)
            return;

        switch (eventData.Animation)
        {
            case AgentAnimationKind.Idle:
                SetFlags(idle: true, thinking: false, talking: false);
                break;
            case AgentAnimationKind.Thinking:
                SetFlags(idle: false, thinking: true, talking: false);
                break;
            case AgentAnimationKind.Talking:
                SetFlags(idle: false, thinking: false, talking: true);
                break;
            case AgentAnimationKind.Celebrate:
                SetFlags(idle: false, thinking: false, talking: false);
                _animator.SetTrigger(_celebrateHash);
                break;
            default:
                SetFlags(idle: false, thinking: false, talking: false);
                break;
        }
    }

    void HandleSpeechFinished()
    {
        var dialogue = ResolveDialogue();
        if (dialogue != null && dialogue.IsSubtitleVisible)
            return;

        GoIdle();
    }

    void HandleSubtitleDismissed()
    {
        if (_animator != null && _animator.GetBool(_thinkingHash))
            return;

        GoIdle();
    }

    void GoIdle()
    {
        if (_animator == null)
            return;

        SetFlags(idle: true, thinking: false, talking: false);
    }

    AgentDialogue ResolveDialogue() =>
        _agentDialogue != null ? _agentDialogue : AgentDialogue.Instance;

    void SetFlags(bool idle, bool thinking, bool talking)
    {
        _animator.SetBool(_idleHash, idle);
        _animator.SetBool(_thinkingHash, thinking);
        _animator.SetBool(_talkingHash, talking);
    }

    void RebuildHashes()
    {
        _idleHash = Animator.StringToHash(_idleBool);
        _thinkingHash = Animator.StringToHash(_thinkingBool);
        _talkingHash = Animator.StringToHash(_talkingBool);
        _celebrateHash = Animator.StringToHash(_celebrateTrigger);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        RebuildHashes();
    }
#endif
}
