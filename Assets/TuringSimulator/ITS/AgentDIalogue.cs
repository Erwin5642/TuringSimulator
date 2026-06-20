// AgentDialogue.cs
// Central hub for all agent communication.
// Receives LLM replies from ITSClient, speaks them via AgentTTS,
// shows them as subtitles via typewriter, and handles voice/text input
// from the student via VoiceInputHandler.
//
// COMPONENT SETUP:
//   Add to your agent character GameObject alongside AgentTTS.
//   Assign UI references in the Inspector.
//   VoiceInputHandler and AgentTTS are found via their singletons.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ITS;

[DefaultExecutionOrder(-100)]
public class AgentDialogue : MonoBehaviour
{
    public static AgentDialogue Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Subtitle bubble")]
    [SerializeField] private GameObject     _bubbleRoot;
    [SerializeField] private TMP_Text       _bubbleText;
    [SerializeField] private float          _typewriterSpeed  = 40f;   // chars/sec
    [SerializeField] private float          _autoDismissAfter = 7f;

    [Header("Voice input UI")]
    [SerializeField] private Button         _micButton;
    [Tooltip("Icon shown while the mic is active.")]
    [SerializeField] private GameObject     _micActiveIndicator;
    [Tooltip("Live partial transcription label (optional).")]
    [SerializeField] private TMP_Text       _partialLabel;

    [Header("Text input (fallback)")]
    [SerializeField] private GameObject     _askPanelRoot;
    [SerializeField] private TMP_InputField _askInput;
    [SerializeField] private Button         _askSendButton;
    [SerializeField] private Button         _askCancelButton;

    [Header("Hint button")]
    [SerializeField] private Button         _hintButton;

    [Header("Loading")]
    [SerializeField] private GameObject     _loadingIndicator;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fires when the dialogue is waiting for a server response.</summary>
    public event Action OnThinkingStarted;

    /// <summary>Fires when the server response has arrived (or errored).</summary>
    public event Action OnThinkingFinished;

    // ── State ─────────────────────────────────────────────────────────────────

    private Coroutine _typewriterRoutine;
    private Coroutine _dismissRoutine;
    private bool      _micToggle;   // true = currently listening

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // ── ITSClient events ──────────────────────────────────────────────────
        if (ITSClient.Instance != null)
        {
            ITSClient.Instance.OnAgentComment += SayAndSpeak;
            ITSClient.Instance.OnAskReply     += SayAndSpeak;
            ITSClient.Instance.OnHintReply    += OnHintReceived;
            ITSClient.Instance.OnServerError  += OnServerError;
        }

        // ── Voice input events ────────────────────────────────────────────────
        if (VoiceInputHandler.Instance != null)
        {
            VoiceInputHandler.Instance.OnTranscriptionReady  += OnTranscriptionReady;
            VoiceInputHandler.Instance.OnPartialTranscription += OnPartialTranscription;
            VoiceInputHandler.Instance.OnListeningStarted    += OnListeningStarted;
            VoiceInputHandler.Instance.OnListeningStopped    += OnListeningStopped;
        }

        // ── TTS events ────────────────────────────────────────────────────────
        if (AgentTTS.Instance != null)
        {
            // Stop the typewriter when TTS finishes so timing stays in sync
            AgentTTS.Instance.OnSpeechFinished += OnTTSFinished;
        }

        // ── UI buttons ────────────────────────────────────────────────────────
        _micButton?.onClick.AddListener(OnMicButtonPressed);
        _askSendButton?.onClick.AddListener(OnAskSend);
        _askCancelButton?.onClick.AddListener(CloseAskPanel);
        _hintButton?.onClick.AddListener(OnHintButtonPressed);

        // Initial UI state — use SetActive directly to avoid firing events at startup
        _bubbleRoot?.SetActive(false);
        _askPanelRoot?.SetActive(false);
        _loadingIndicator?.SetActive(false);
        _micActiveIndicator?.SetActive(false);
        if (_partialLabel != null) _partialLabel.text = "";
    }

    private void OnDestroy()
    {
        if (ITSClient.Instance != null)
        {
            ITSClient.Instance.OnAgentComment -= SayAndSpeak;
            ITSClient.Instance.OnAskReply     -= SayAndSpeak;
            ITSClient.Instance.OnHintReply    -= OnHintReceived;
            ITSClient.Instance.OnServerError  -= OnServerError;
        }
        if (VoiceInputHandler.Instance != null)
        {
            VoiceInputHandler.Instance.OnTranscriptionReady   -= OnTranscriptionReady;
            VoiceInputHandler.Instance.OnPartialTranscription -= OnPartialTranscription;
            VoiceInputHandler.Instance.OnListeningStarted     -= OnListeningStarted;
            VoiceInputHandler.Instance.OnListeningStopped     -= OnListeningStopped;
        }
        if (AgentTTS.Instance != null)
            AgentTTS.Instance.OnSpeechFinished -= OnTTSFinished;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>Show subtitle + speak via TTS.</summary>
    public void SayAndSpeak(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        SetThinking(false);
        ShowSubtitle(message);
        AgentTTS.Instance?.Speak(message);
    }

    /// <summary>Show subtitle only (no TTS).</summary>
    public void ShowSubtitle(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (_typewriterRoutine != null) StopCoroutine(_typewriterRoutine);
        if (_dismissRoutine    != null) StopCoroutine(_dismissRoutine);
        _bubbleRoot?.SetActive(true);
        _typewriterRoutine = StartCoroutine(TypewriterRoutine(message));
    }

    /// <summary>Open the text-input ask panel (fallback when voice is unavailable).</summary>
    public void OpenAskPanel()
    {
        _askPanelRoot?.SetActive(true);
        _askInput?.Select();
    }

    public void CloseAskPanel()
    {
        _askPanelRoot?.SetActive(false);
        if (_askInput != null) _askInput.text = "";
    }

    // ── Mic button ────────────────────────────────────────────────────────────

    private void OnMicButtonPressed()
    {
        if (VoiceInputHandler.Instance == null) return;

        // Stop agent speech before listening — avoids feedback loop
        AgentTTS.Instance?.Stop();

        if (!_micToggle)
            VoiceInputHandler.Instance.StartListening();
        else
            VoiceInputHandler.Instance.StopListening();
    }

    // ── Voice input callbacks ─────────────────────────────────────────────────

    private void OnListeningStarted()
    {
        _micToggle = true;
        _micActiveIndicator?.SetActive(true);
        if (_partialLabel != null) _partialLabel.text = "";
    }

    private void OnListeningStopped()
    {
        _micToggle = false;
        _micActiveIndicator?.SetActive(false);
        SetThinking(true);   // waiting for server
        if (_partialLabel != null) _partialLabel.text = "";
    }

    private void OnPartialTranscription(string partial)
    {
        // Show live subtitle of what Wit.ai is hearing
        if (_partialLabel != null)
            _partialLabel.text = partial;
    }

    private void OnTranscriptionReady(string text)
    {
        if (_partialLabel != null) _partialLabel.text = "";

        // Send the transcribed Portuguese text to the ITS server as a question
        string studentId = SkillTracker.Instance?.StudentId ?? "student_default";
        string levelId   = SkillTracker.Instance?.GetCurrentLevelId() ?? "";

        ITSClient.Instance?.Ask(studentId, levelId, text);
    }

    // ── Hint button ───────────────────────────────────────────────────────────

    private void OnHintButtonPressed()
    {
        // Stop agent speech so the hint is heard clearly
        AgentTTS.Instance?.Stop();
        SetThinking(true);

        string studentId = SkillTracker.Instance?.StudentId ?? "student_default";
        string levelId   = SkillTracker.Instance?.GetCurrentLevelId() ?? "";

        ITSClient.Instance?.RequestHint(studentId, levelId, null);
    }

    // ── Server response callbacks ─────────────────────────────────────────────

    private void OnHintReceived(HintResponse resp)
    {
        SayAndSpeak(resp.reply);
        AgentAnimator.Instance?.TriggerHint();
        Debug.Log($"[AgentDialogue] Hint — skill: {resp.skill_id}, level: {resp.hint_level}");
    }

    private void OnServerError(string error)
    {
        SetThinking(false);
        SayAndSpeak("Hmm, parece que perdi o sinal. Tente novamente em um momento, treineiro.");
        Debug.LogWarning($"[AgentDialogue] Server error: {error}");
    }

    // ── TTS callback ──────────────────────────────────────────────────────────

    private void OnTTSFinished()
    {
        // Auto-dismiss bubble shortly after TTS ends
        if (_dismissRoutine != null) StopCoroutine(_dismissRoutine);
        _dismissRoutine = StartCoroutine(AutoDismiss(1.5f));
    }

    // ── Text-input fallback ───────────────────────────────────────────────────

    private void OnAskSend()
    {
        if (_askInput == null || string.IsNullOrWhiteSpace(_askInput.text)) return;
        string question = _askInput.text.Trim();
        CloseAskPanel();
        SetThinking(true);

        string studentId = SkillTracker.Instance?.StudentId ?? "student_default";
        string levelId   = SkillTracker.Instance?.GetCurrentLevelId() ?? "";

        ITSClient.Instance?.Ask(studentId, levelId, question);
    }

    // ── Thinking state helper ─────────────────────────────────────────────────

    private void SetThinking(bool thinking)
    {
        _loadingIndicator?.SetActive(thinking);
        if (thinking) OnThinkingStarted?.Invoke();
        else          OnThinkingFinished?.Invoke();
    }

    // ── Typewriter + dismiss ──────────────────────────────────────────────────

    private IEnumerator TypewriterRoutine(string message)
    {
        _bubbleText.text = "";
        float delay = 1f / _typewriterSpeed;
        foreach (char c in message)
        {
            _bubbleText.text += c;
            yield return new WaitForSeconds(delay);
        }
        // If TTS is not speaking, auto-dismiss after a fixed delay
        if (AgentTTS.Instance == null || !AgentTTS.Instance.IsSpeaking)
            _dismissRoutine = StartCoroutine(AutoDismiss(_autoDismissAfter));
    }

    private IEnumerator AutoDismiss(float delay)
    {
        yield return new WaitForSeconds(delay);
        _bubbleRoot?.SetActive(false);
    }
}