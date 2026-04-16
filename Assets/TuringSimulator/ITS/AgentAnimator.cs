// AgentAnimator.cs
// Drives the agent's Animator state machine from ITS events.
//
// ANIMATOR CONTROLLER SETUP — create these in your Animator window:
//
//   Parameters (add all of these):
//     bool  IsTalking     — true while TTS is speaking
//     bool  IsThinking    — true while waiting for server response
//     trigger Celebrate   — fired on level complete / correct answer
//     trigger Hint        — fired when a hint is delivered
//
//   States (minimum required):
//     Idle        — default base state, no conditions
//     Talking     — IsTalking = true
//     Thinking    — IsThinking = true
//     Celebrate   — triggered by Celebrate, transitions back to Idle
//     Hint        — triggered by Hint, transitions back to Talking or Idle
//
//   Recommended transition setup:
//     Any State  → Thinking    : IsThinking = true   (priority: high)
//     Any State  → Talking     : IsTalking = true, IsThinking = false
//     Talking    → Idle        : IsTalking = false
//     Thinking   → Idle        : IsThinking = false
//     Any State  → Celebrate   : Celebrate trigger
//     Any State  → Hint        : Hint trigger
//     Celebrate  → Idle        : Exit Time 1.0
//     Hint       → Idle        : Exit Time 1.0
//
// COMPONENT SETUP:
//   Add this script to the same GameObject as your Animator component.
//   No Inspector wiring needed — it finds AgentTTS and AgentDialogue
//   via their singletons on Start.

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AgentAnimator : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────

    public static AgentAnimator Instance { get; private set; }

    // ── Animator parameter name constants ─────────────────────────────────────
    // Match these exactly to the parameter names in your Animator Controller.

    private static readonly int ParamIsTalking  = Animator.StringToHash("IsTalking");
    private static readonly int ParamIsThinking = Animator.StringToHash("IsThinking");
    private static readonly int ParamCelebrate  = Animator.StringToHash("Celebrate");
    private static readonly int ParamHint       = Animator.StringToHash("Hint");

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Lip sync")]
    [Tooltip("When enabled, the jaw bone is driven procedurally from TTS audio volume.")]
    [SerializeField] private bool       _enableLipSync      = true;
    [SerializeField] private Transform  _jawBone;
    [Tooltip("Maximum jaw open angle in degrees.")]
    [SerializeField] private float      _jawMaxAngle        = 18f;
    [Tooltip("How quickly the jaw follows audio volume.")]
    [SerializeField] private float      _jawFollowSpeed     = 18f;

    [Header("Thinking loop")]
    [Tooltip("How long the agent stays in Thinking before falling back to Idle " +
             "if the server never responds (safety timeout).")]
    [SerializeField] private float      _thinkingTimeout    = 12f;

    // ── Internals ─────────────────────────────────────────────────────────────

    private Animator   _animator;
    private float      _currentJawAngle;
    private Coroutine  _thinkingTimeoutRoutine;
    private bool       _isTalking;
    private bool       _isThinking;

    // Smoothed audio amplitude for lip sync
    private float      _smoothAmplitude;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // AgentTTS events
        if (AgentTTS.Instance != null)
        {
            AgentTTS.Instance.OnSpeechStarted  += OnSpeechStarted;
            AgentTTS.Instance.OnSpeechFinished += OnSpeechFinished;
        }

        // AgentDialogue events — thinking state is driven by loading indicator
        if (AgentDialogue.Instance != null)
        {
            AgentDialogue.Instance.OnThinkingStarted  += OnThinkingStarted;
            AgentDialogue.Instance.OnThinkingFinished += OnThinkingFinished;
        }

        // ITSClient events — celebrate on level complete
        if (ITSClient.Instance != null)
        {
            ITSClient.Instance.OnAgentComment += OnAgentCommentReceived;
        }

        // SkillTracker event — celebrate on level complete
        if (SkillTracker.Instance != null)
        {
            SkillTracker.Instance.OnLevelCompleteAnimEvent += TriggerCelebrate;
        }
    }

    private void OnDestroy()
    {
        if (AgentTTS.Instance != null)
        {
            AgentTTS.Instance.OnSpeechStarted  -= OnSpeechStarted;
            AgentTTS.Instance.OnSpeechFinished -= OnSpeechFinished;
        }
        if (AgentDialogue.Instance != null)
        {
            AgentDialogue.Instance.OnThinkingStarted  -= OnThinkingStarted;
            AgentDialogue.Instance.OnThinkingFinished -= OnThinkingFinished;
        }
        if (ITSClient.Instance != null)
        {
            ITSClient.Instance.OnAgentComment -= OnAgentCommentReceived;
        }
        if (SkillTracker.Instance != null)
        {
            SkillTracker.Instance.OnLevelCompleteAnimEvent -= TriggerCelebrate;
        }
    }

    private void Update()
    {
        if (_enableLipSync && _isTalking)
            UpdateLipSync();
    }

    // ── TTS → animation ───────────────────────────────────────────────────────

    private void OnSpeechStarted(string text)
    {
        _isTalking = true;
        _animator.SetBool(ParamIsTalking,  true);
        _animator.SetBool(ParamIsThinking, false);
        StopThinkingTimeout();
    }

    private void OnSpeechFinished()
    {
        _isTalking = false;
        _animator.SetBool(ParamIsTalking, false);

        // Smoothly close jaw
        StartCoroutine(CloseJawRoutine());
    }

    // ── Server wait → thinking animation ─────────────────────────────────────

    private void OnThinkingStarted()
    {
        _isThinking = true;
        _animator.SetBool(ParamIsThinking, true);
        _thinkingTimeoutRoutine = StartCoroutine(ThinkingTimeoutRoutine());
    }

    private void OnThinkingFinished()
    {
        _isThinking = false;
        _animator.SetBool(ParamIsThinking, false);
        StopThinkingTimeout();
    }

    // ── Triggers ──────────────────────────────────────────────────────────────

    public void TriggerCelebrate()
    {
        _animator.SetBool(ParamIsThinking, false);
        _animator.SetTrigger(ParamCelebrate);
    }

    public void TriggerHint()
    {
        _animator.SetTrigger(ParamHint);
    }

    // When the server returns a comment, check if it is hint-related
    private void OnAgentCommentReceived(string comment)
    {
        // Hint replies are handled via ITSClient.OnHintReply in AgentDialogue —
        // this handler catches reactive event comments (program fail/success).
        // No additional trigger needed here; talking state covers it.
    }

    // ── Lip sync ──────────────────────────────────────────────────────────────

    private void UpdateLipSync()
    {
        if (_jawBone == null) return;

        // Sample microphone / audio output amplitude
        // Android TTS plays through the device speaker, not a Unity AudioSource,
        // so we use a simple sine-wave approximation driven by time.
        // Replace this with real audio data if you add an AudioSource to the TTS.
        float rawAmplitude = Mathf.Abs(Mathf.Sin(Time.time * 8f)) * 0.6f
                           + Mathf.Abs(Mathf.Sin(Time.time * 13f)) * 0.4f;

        _smoothAmplitude = Mathf.Lerp(
            _smoothAmplitude, rawAmplitude, Time.deltaTime * _jawFollowSpeed);

        float targetAngle = _smoothAmplitude * _jawMaxAngle;
        _currentJawAngle  = Mathf.Lerp(
            _currentJawAngle, targetAngle, Time.deltaTime * _jawFollowSpeed);

        _jawBone.localRotation = Quaternion.Euler(_currentJawAngle, 0f, 0f);
    }

    private IEnumerator CloseJawRoutine()
    {
        while (_currentJawAngle > 0.5f)
        {
            _currentJawAngle = Mathf.Lerp(_currentJawAngle, 0f,
                Time.deltaTime * _jawFollowSpeed);
            if (_jawBone != null)
                _jawBone.localRotation = Quaternion.Euler(_currentJawAngle, 0f, 0f);
            yield return null;
        }
        _currentJawAngle = 0f;
        if (_jawBone != null)
            _jawBone.localRotation = Quaternion.identity;
    }

    // ── Thinking timeout ──────────────────────────────────────────────────────

    private IEnumerator ThinkingTimeoutRoutine()
    {
        yield return new WaitForSeconds(_thinkingTimeout);
        if (_isThinking)
        {
            Debug.LogWarning("[AgentAnimator] Thinking timeout — forcing Idle.");
            OnThinkingFinished();
        }
    }

    private void StopThinkingTimeout()
    {
        if (_thinkingTimeoutRoutine != null)
        {
            StopCoroutine(_thinkingTimeoutRoutine);
            _thinkingTimeoutRoutine = null;
        }
    }
}