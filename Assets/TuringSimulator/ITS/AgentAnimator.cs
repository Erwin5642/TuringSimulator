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
    }

    void OnDisable()
    {
        if (_agentActionChannel != null)
            _agentActionChannel.OnRaised -= HandleActionRequested;

        if (AgentTTS.Instance != null)
            AgentTTS.Instance.OnSpeechFinished -= HandleSpeechFinished;
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
        if (_animator == null)
            return;

        _animator.SetBool(_talkingHash, false);
    }

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
